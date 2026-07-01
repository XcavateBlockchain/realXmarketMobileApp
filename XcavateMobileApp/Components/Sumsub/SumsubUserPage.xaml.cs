using PlutoFramework.Components.Card;
using PlutoFramework.Components.Sumsub;
using PlutoFramework.Model.Sumsub;
using PlutoFramework.Templates.PageTemplate;
using System.Globalization;

namespace XcavateMobileApp.Components.Sumsub
{
    /// <summary>
    /// Displays Sumsub KYC information for a user identified by their Substrate key.
    /// Uses Sumsub status components for displaying verified/rejected/needsResubmit states.
    /// </summary>
    public partial class SumsubUserPage : PageTemplate
    {
        /// <summary>
        /// Creates a new SumsubUserPage for the given Substrate key (wallet address).
        /// </summary>
        /// <param name="substrateKey">The Substrate address used as the external user ID in Sumsub.</param>
        public SumsubUserPage(string substrateKey)
        {
            InitializeComponent();
            this.Loaded += async (s, e) => await LoadUserDataAsync(substrateKey);
        }

        private async Task LoadUserDataAsync(string substrateKey)
        {
            try
            {
                LoadingIndicator.IsRunning = true;
                LoadingIndicator.IsVisible = true;

                var secrets = SumsubSecretModel.GetSecrets();
                var applicant = await SumsubModel.GetApplicantDataAsync(
                    substrateKey,
                    secrets.SecretKey,
                    secrets.AppToken,
                    CancellationToken.None
                );

                if (applicant is null)
                {
                    ErrorLabel.Text = "No KYC data found for this Substrate key.";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                var status = SumsubStatusModelParser.ParseStatus(applicant);
                var enhancedData = await LoadEnhancedTimelineDataAsync(applicant, secrets.SecretKey, secrets.AppToken, CancellationToken.None);

                Console.WriteLine("Sumsub Status:");
                Console.WriteLine(status);

                PopulateUserInfo(applicant, substrateKey);
                ShowStatusComponent(status);
                PopulateVerificationStatus(applicant);
                BuildTimeline(applicant, enhancedData);

                UserInfoCard.IsVisible = true;
                StatusCard.IsVisible = true;
                if (StatusComponentLayout.IsVisible)
                    StatusComponentLayout.IsVisible = true;
                if (TimelineLayout.Children.Count > 0)
                    Timeline.IsVisible = true;
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error loading user data: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }

        private void PopulateVerificationStatus(SumsubApplicant applicant)
        {
            var review = applicant.Review;
            if (review != null)
            {
                StatusLabel.Text = $"Status: {review.ReviewStatus ?? "Unknown"}";
                RoleLabel.Text = $"Role: {review.LevelName ?? "Not assigned"}";
                AttemptsLabel.Text = $"Attempts: {review.AttemptCnt ?? 0}";
                PriorityLabel.Text = $"Priority: {(review.Priority.HasValue ? review.Priority.ToString() : "Default")}";
            }
            else
            {
                StatusLabel.Text = "Status: No review found";
                RoleLabel.Text = "Role: Not assigned";
                AttemptsLabel.Text = "Attempts: 0";
                PriorityLabel.Text = "Priority: Default";
            }
        }

        private void ShowStatusComponent(SumsubStatusData status)
        {
            StatusComponentLayout.IsVisible = true;

            // Remove any existing components from the container
            StatusComponentContainer.Content = null;



            ContentView? component = status.StatusType switch
            {
                SumsubStatusType.Approved =>
                    CreateApprovedComponent(status),
                SumsubStatusType.Rejected =>
                    CreateRejectedComponent(status),
                SumsubStatusType.NeedsResubmit =>
                    CreateNeedsResubmitComponent(status),
                SumsubStatusType.Pending =>
                    CreatePendingComponent(status),
                _ => null
            };

            if (component != null)
            {
                StatusComponentContainer.Content = component;
            }
        }

        private ContentView CreateApprovedComponent(SumsubStatusData status)
        {
            var view = new SumsubApprovedView();
            view.Bind(status);
            return view;
        }

        private ContentView CreateRejectedComponent(SumsubStatusData status)
        {
            var view = new SumsubRejectedView();
            view.Bind(status);
            return view;
        }

        private ContentView CreateNeedsResubmitComponent(SumsubStatusData status)
        {
            var view = new SumsubNeedsResubmitView();
            view.Bind(status);
            return view;
        }

        private ContentView CreatePendingComponent(SumsubStatusData status)
        {
            // Simple pending indicator
            var layout = new VerticalStackLayout
            {
                Spacing = 5,
                Padding = 15
            };

            layout.Add(new Label
            {
                Text = "Your verification is being reviewed.",
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Fill
            });

            layout.Add(new Label
            {
                Text = $"Submitted on {status.Timestamp:dddd, dd MMMM yyyy}",
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Fill
            });

            return new ContentView { Content = layout };
        }

        private static async Task<EnhancedTimelineData> LoadEnhancedTimelineDataAsync(
            SumsubApplicant applicant,
            string secretKey,
            string appToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(applicant.Id))
            {
                return new EnhancedTimelineData();
            }

            var reviewStatusTask = SafeLoadAsync(() => SumsubModel.GetApplicantReviewStatusAsync(applicant.Id, secretKey, appToken, cancellationToken));
            var reviewHistoryTask = SafeLoadAsync(() => SumsubModel.GetApplicantReviewHistoryAsync(applicant.Id, secretKey, appToken, cancellationToken));
            var stepStatusTask = SafeLoadAsync(() => SumsubModel.GetApplicantVerificationStepsStatusAsync(applicant.Id, secretKey, appToken, cancellationToken));
            var notesTask = SafeLoadAsync(() => SumsubModel.GetApplicantNotesAsync(applicant.Id, secretKey, appToken, cancellationToken));

            await Task.WhenAll(reviewStatusTask, reviewHistoryTask, stepStatusTask, notesTask);

            return new EnhancedTimelineData(
                reviewStatusTask.Result,
                reviewHistoryTask.Result,
                stepStatusTask.Result,
                notesTask.Result);
        }

        private static async Task<T?> SafeLoadAsync<T>(Func<Task<T?>> loader)
        {
            try
            {
                return await loader();
            }
            catch
            {
                return default;
            }
        }

        private void PopulateUserInfo(SumsubApplicant applicant, string substrateKey)
        {
            SubstrateKeyLabel.Text = $"Substrate Key: {substrateKey}";
            ApplicantIdLabel.Text = $"Applicant ID: {applicant.Id ?? "Unknown"}";
            EmailLabel.Text = $"Email: {applicant.Email ?? "Not provided"}";
            PhoneLabel.Text = $"Phone: {applicant.Phone ?? "Not provided"}";
            PlatformLabel.Text = $"Platform: {applicant.ApplicantPlatform}";
        }

        /// <summary>
        /// Builds a chronological timeline of KYC events from the applicant data.
        /// Timeline entries include application creation, review initiation,
        /// and verification status changes.
        /// </summary>
        private void BuildTimeline(SumsubApplicant applicant, EnhancedTimelineData enhancedData)
        {
            TimelineLayout.Clear();
            var timelineItems = BuildTimelineEvents(applicant, enhancedData);

            if (timelineItems.Count == 0)
            {
                TimelineLayout.Add(BuildEmptyTimelineEntry());
                return;
            }

            // Newest first to match Sumsub dashboard style.
            timelineItems.Sort((a, b) =>
            {
                if (a.Date.HasValue && b.Date.HasValue) return b.Date.Value.CompareTo(a.Date.Value);
                if (a.Date.HasValue) return -1;
                if (b.Date.HasValue) return 1;
                return 0;
            });

            DateTime? previousDatedEvent = null;
            for (int i = 0; i < timelineItems.Count; i++)
            {
                var current = timelineItems[i];
                var gap = string.Empty;

                if (i == 0)
                {

                }
                else if (current.Date.HasValue && previousDatedEvent.HasValue)
                {
                    gap = FormatTimeGap(previousDatedEvent.Value - current.Date.Value);
                }

                TimelineLayout.Add(BuildTimelineEntry(
                    current,
                    gap,
                    isFirst: i == 0,
                    isLast: i == timelineItems.Count - 1));

                if (current.Date.HasValue)
                {
                    previousDatedEvent = current.Date.Value;
                }
            }
        }

        private List<TimelineEventItem> BuildTimelineEvents(SumsubApplicant applicant, EnhancedTimelineData enhancedData)
        {
            var timelineItems = new List<TimelineEventItem>();

            var createdDate = ParseSumsubDate(applicant.CreatedAt);
            timelineItems.Add(new TimelineEventItem(
                createdDate,
                "Application created",
                "success",
                !string.IsNullOrWhiteSpace(applicant.CreatedBy) ? $"By: {applicant.CreatedBy}" : null,
                new List<string>
                {
                    $"Platform: {applicant.ApplicantPlatform}",
                    !string.IsNullOrWhiteSpace(applicant.ExternalUserId) ? $"External user: {applicant.ExternalUserId}" : string.Empty,
                }));

            if (!string.IsNullOrWhiteSpace(applicant.Type))
            {
                timelineItems.Add(new TimelineEventItem(
                    null,
                    $"Applicant type: {applicant.Type}",
                    "info"));
            }

            if (applicant.RequiredIdDocs?.DocSets != null)
            {
                foreach (var docSet in applicant.RequiredIdDocs.DocSets)
                {
                    var details = new List<string>();
                    if (docSet.Types != null && docSet.Types.Count > 0)
                    {
                        details.Add($"Allowed docs: {string.Join(", ", docSet.Types)}");
                    }

                    if (!string.IsNullOrWhiteSpace(docSet.VideoRequired))
                    {
                        details.Add($"Video required: {docSet.VideoRequired}");
                    }

                    timelineItems.Add(new TimelineEventItem(
                        null,
                        $"Document set required: {docSet.IdDocSetType ?? "General"}",
                        "info",
                        null,
                        details));
                }
            }

            var review = applicant.Review;
            if (review != null)
            {
                var reviewDate = ParseSumsubDate(review.CreateDate);
                var reviewStatus = string.IsNullOrWhiteSpace(review.ReviewStatus) ? "created" : review.ReviewStatus;
                var statusCategory = (review.ReviewStatus ?? string.Empty).ToLowerInvariant() switch
                {
                    "approved" => "success",
                    "rejected" => "error",
                    "onhold" => "warning",
                    "pending" => "warning",
                    _ => "info"
                };

                var reviewDetails = new List<string>();
                if (!string.IsNullOrWhiteSpace(review.ReviewId)) reviewDetails.Add($"Review ID: {review.ReviewId}");
                if (!string.IsNullOrWhiteSpace(review.AttemptId)) reviewDetails.Add($"Attempt ID: {review.AttemptId}");

                timelineItems.Add(new TimelineEventItem(
                    reviewDate,
                    $"Review {reviewStatus.ToLowerInvariant()}",
                    statusCategory,
                    !string.IsNullOrWhiteSpace(review.LevelName) ? $"Level: {review.LevelName}" : null,
                    reviewDetails));

                if (review.AttemptCnt.HasValue)
                {
                    timelineItems.Add(new TimelineEventItem(
                        null,
                        $"Verification attempts: {review.AttemptCnt}",
                        review.AttemptCnt > 1 ? "warning" : "info"));
                }

                if (!string.IsNullOrWhiteSpace(review.LevelAutoCheckMode))
                {
                    timelineItems.Add(new TimelineEventItem(
                        null,
                        $"Auto-check mode: {review.LevelAutoCheckMode}",
                        "info"));
                }

                if (review.Priority.HasValue)
                {
                    timelineItems.Add(new TimelineEventItem(
                        null,
                        $"Priority: {review.Priority.Value}",
                        review.Priority.Value > 0 ? "warning" : "info"));
                }
            }

            if (enhancedData.ReviewStatus != null)
            {
                var details = new List<string>();
                if (enhancedData.ReviewStatus.ReviewResult?.RejectLabels?.Count > 0)
                {
                    details.Add($"Reject labels: {string.Join(", ", enhancedData.ReviewStatus.ReviewResult.RejectLabels)}");
                }

                if (!string.IsNullOrWhiteSpace(enhancedData.ReviewStatus.ReviewResult?.ModerationComment))
                {
                    details.Add($"Moderation: {enhancedData.ReviewStatus.ReviewResult.ModerationComment}");
                }

                var reviewDate = ParseSumsubDate(enhancedData.ReviewStatus.ReviewDate)
                    ?? ParseSumsubDate(enhancedData.ReviewStatus.CreateDate);
                timelineItems.Add(new TimelineEventItem(
                    reviewDate,
                    $"Current review status: {enhancedData.ReviewStatus.ReviewStatus ?? "unknown"}",
                    GetStatusCategoryFromReviewAnswer(enhancedData.ReviewStatus.ReviewResult?.ReviewAnswer, enhancedData.ReviewStatus.ReviewStatus),
                    !string.IsNullOrWhiteSpace(enhancedData.ReviewStatus.LevelName) ? $"Level: {enhancedData.ReviewStatus.LevelName}" : null,
                    details));
            }

            if (enhancedData.ReviewHistory?.Items != null)
            {
                foreach (var history in enhancedData.ReviewHistory.Items)
                {
                    var details = new List<string>();
                    if (history.ReviewResult?.RejectLabels?.Count > 0)
                    {
                        details.Add($"Reject labels: {string.Join(", ", history.ReviewResult.RejectLabels)}");
                    }

                    if (!string.IsNullOrWhiteSpace(history.ReviewResult?.ModerationComment))
                    {
                        details.Add($"Moderation: {history.ReviewResult.ModerationComment}");
                    }

                    timelineItems.Add(new TimelineEventItem(
                        ParseSumsubDate(history.ReviewDate),
                        $"Review attempt {history.AttemptId ?? "unknown"}: {history.ReviewStatus ?? "unknown"}",
                        GetStatusCategoryFromReviewAnswer(history.ReviewResult?.ReviewAnswer, history.ReviewStatus),
                        !string.IsNullOrWhiteSpace(history.LevelName) ? $"Level: {history.LevelName}" : null,
                        details));
                }
            }

            if (enhancedData.StepStatuses != null)
            {
                foreach (var step in enhancedData.StepStatuses)
                {
                    var status = step.Value;
                    var details = new List<string>();

                    if (!string.IsNullOrWhiteSpace(status.IdDocType)) details.Add($"Document: {status.IdDocType}");
                    if (!string.IsNullOrWhiteSpace(status.Country)) details.Add($"Country: {status.Country}");
                    if (status.ImageIds?.Count > 0) details.Add($"Images uploaded: {status.ImageIds.Count}");
                    if (!string.IsNullOrWhiteSpace(status.ReviewResult?.ModerationComment)) details.Add($"Moderation: {status.ReviewResult.ModerationComment}");
                    if (status.ReviewResult?.RejectLabels?.Count > 0) details.Add($"Reject labels: {string.Join(", ", status.ReviewResult.RejectLabels)}");

                    timelineItems.Add(new TimelineEventItem(
                        null,
                        $"Step {step.Key}: {status.ReviewResult?.ReviewAnswer ?? status.ReviewStatus ?? "pending"}",
                        GetStatusCategoryFromReviewAnswer(status.ReviewResult?.ReviewAnswer, status.ReviewStatus),
                        null,
                        details));
                }
            }

            if (enhancedData.Notes?.List?.Items != null)
            {
                foreach (var note in enhancedData.Notes.List.Items)
                {
                    if (string.IsNullOrWhiteSpace(note.Note))
                    {
                        continue;
                    }

                    timelineItems.Add(new TimelineEventItem(
                        ParseSumsubDate(note.CreatedAt),
                        "Reviewer note added",
                        "info",
                        !string.IsNullOrWhiteSpace(note.CreatedBy) ? $"By: {note.CreatedBy}" : null,
                        new List<string> { note.Note }));
                }
            }

            return timelineItems;
        }

        private static DateTime? ParseSumsubDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var formats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss+0000",
                "yyyy-MM-dd HH:mm:sszzz",
                "yyyy-MM-ddTHH:mm:ssZ",
                "yyyy-MM-ddTHH:mm:ss.fffZ",
                "yyyy-MM-ddTHH:mm:ss.fffffffZ",
            };

            if (DateTimeOffset.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dto))
            {
                return dto.UtcDateTime;
            }

            if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            {
                return parsed;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string GetStatusCategoryFromReviewAnswer(string? reviewAnswer, string? reviewStatus)
        {
            if (!string.IsNullOrWhiteSpace(reviewAnswer))
            {
                return reviewAnswer.ToUpperInvariant() switch
                {
                    "GREEN" => "success",
                    "RED" => "error",
                    "YELLOW" => "warning",
                    _ => "info"
                };
            }

            return GetStatusCategoryFromReviewStatus(reviewStatus);
        }

        private static string GetStatusCategoryFromReviewStatus(string? reviewStatus)
        {
            return (reviewStatus ?? string.Empty).ToLowerInvariant() switch
            {
                "completed" => "success",
                "pending" => "warning",
                "queued" => "warning",
                "prechecked" => "warning",
                "onhold" => "warning",
                "awaitingservice" => "warning",
                "awaitinguser" => "warning",
                _ => "info"
            };
        }

        private View BuildTimelineEntry(TimelineEventItem item, string timeGapText, bool isFirst, bool isLast)
        {
            var dotColor = item.Status switch
            {
                "success" => Color.FromArgb("#357461"),
                "error" => Color.FromArgb("#dc7da6"),
                "warning" => Color.FromArgb("#d39d3f"),
                _ => Color.FromArgb("#919191")
            };

            var wrapper = new Grid
            {
                ColumnSpacing = 10,
                ColumnDefinitions =
                {
                    new ColumnDefinition(64),
                    new ColumnDefinition(GridLength.Star)
                },
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Auto)
                }
            };

            var gapLabel = new Label
            {
                Text = timeGapText,
                FontSize = 11,
                TextColor = Color.FromArgb("#6E6E6E"),
                HorizontalTextAlignment = TextAlignment.Start,
                VerticalTextAlignment = TextAlignment.End,
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 1,
                IsVisible = !string.IsNullOrWhiteSpace(timeGapText)
            };
            wrapper.Add(gapLabel, 0, 0);

            var markerGrid = new Grid
            {
                WidthRequest = 20,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Fill,
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star)
                }
            };

            markerGrid.Add(new BoxView
            {
                WidthRequest = 2,
                Color = Color.FromArgb("#C8C8C8"),
                HorizontalOptions = LayoutOptions.Center,
                IsVisible = !isFirst
            }, 0, 0);

            markerGrid.Add(new Label
            {
                Text = "\u25cf",
                TextColor = dotColor,
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }, 0, 1);

            markerGrid.Add(new BoxView
            {
                WidthRequest = 2,
                Color = Color.FromArgb("#C8C8C8"),
                HorizontalOptions = LayoutOptions.Center,
                IsVisible = !isLast
            }, 0, 2);

            var railHost = new Grid
            {
                VerticalOptions = LayoutOptions.Fill,
                HorizontalOptions = LayoutOptions.Fill,
                Padding = new Thickness(0, 0, 0, 0)
            };
            railHost.Add(markerGrid);
            wrapper.Add(railHost, 0, 1);

            var content = new VerticalStackLayout { Spacing = 4 };
            var titleRow = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

            titleRow.Add(new Label
            {
                Text = item.Action,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 3
            }, 0, 0);

            titleRow.Add(new Label
            {
                Text = item.Status.ToUpperInvariant(),
                FontSize = 10,
                Padding = new Thickness(8, 2),
                BackgroundColor = dotColor.WithAlpha(0.2f),
                TextColor = dotColor,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            }, 1, 0);

            content.Add(titleRow);

            if (item.Date.HasValue)
            {
                content.Add(new Label
                {
                    Text = item.Date.Value.ToString("MMM dd, yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6E6E6E"),
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            if (!string.IsNullOrWhiteSpace(item.Subtitle))
            {
                content.Add(new Label
                {
                    Text = item.Subtitle,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6E6E6E"),
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            foreach (var detail in item.Details)
            {
                if (string.IsNullOrWhiteSpace(detail))
                {
                    continue;
                }

                content.Add(new Label
                {
                    Text = $"• {detail}",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6E6E6E"),
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            var eventCard = new Card
            {
                CardPadding = new Thickness(12, 10),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Start
            };

            eventCard.View = content;
            wrapper.Add(eventCard, 1, 1);

            markerGrid.SetBinding(HeightRequestProperty, new Binding(nameof(Height), source: eventCard));

            return wrapper;
        }

        private static View BuildEmptyTimelineEntry()
        {
            var emptyCard = new Card
            {
                CardPadding = new Thickness(12, 10)
            };

            emptyCard.View = new Label
            {
                Text = "No timeline events available yet.",
                FontSize = 13,
                TextColor = Color.FromArgb("#6E6E6E")
            };

            return emptyCard;
        }

        private static string FormatTimeGap(TimeSpan gap)
        {
            if (gap.TotalSeconds < 0)
            {
                gap = gap.Negate();
            }

            if (gap.TotalSeconds < 60)
            {
                var seconds = Math.Max(1, (int)Math.Round(gap.TotalSeconds));
                return $"{seconds} second{(seconds == 1 ? string.Empty : "s")}";
            }

            if (gap.TotalMinutes < 60)
            {
                var minutes = Math.Max(1, (int)Math.Round(gap.TotalMinutes));
                return $"{minutes} minute{(minutes == 1 ? string.Empty : "s")}";
            }

            if (gap.TotalHours < 24)
            {
                var hours = Math.Max(1, (int)Math.Round(gap.TotalHours));
                return $"{hours} hour{(hours == 1 ? string.Empty : "s")}";
            }

            var days = Math.Max(1, (int)Math.Round(gap.TotalDays));
            return $"{days} day{(days == 1 ? string.Empty : "s")}";
        }

        private sealed record TimelineEventItem(
            DateTime? Date,
            string Action,
            string Status,
            string? Subtitle = null,
            List<string>? Details = null)
        {
            public List<string> Details { get; } = Details ?? new List<string>();
        }

        private sealed record EnhancedTimelineData(
            SumsubReview? ReviewStatus = null,
            SumsubReviewHistoryResponse? ReviewHistory = null,
            Dictionary<string, SumsubVerificationStepStatus>? StepStatuses = null,
            SumsubApplicantNotesResponse? Notes = null);
    }
}
