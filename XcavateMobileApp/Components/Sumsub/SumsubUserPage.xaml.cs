using PlutoFramework.Model.Sumsub;
using PlutoFramework.Templates.PageTemplate;
using System.Globalization;
using PlutoFramework.Components.Card;

namespace XcavateMobileApp.Components.Sumsub
{
    /// <summary>
    /// Displays Sumsub KYC information for a user identified by their Substrate key.
    /// Shows user details, verification status, and a chronological timeline of KYC actions.
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

                PopulateUserInfo(applicant, substrateKey);
                PopulateVerificationStatus(applicant);
                BuildTimeline(applicant);

                UserInfoCard.IsVisible = true;
                StatusCard.IsVisible = true;
                if (TimelineLayout.Children.Count > 0)
                    TimelineCard.IsVisible = true;
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

        private void PopulateUserInfo(SumsubApplicant applicant, string substrateKey)
        {
            SubstrateKeyLabel.Text = $"Substrate Key: {substrateKey}";
            ApplicantIdLabel.Text = $"Applicant ID: {applicant.Id ?? "Unknown"}";
            EmailLabel.Text = $"Email: {applicant.Email ?? "Not provided"}";
            PhoneLabel.Text = $"Phone: {applicant.Phone ?? "Not provided"}";
            PlatformLabel.Text = $"Platform: {applicant.ApplicantPlatform}";
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

        /// <summary>
        /// Builds a chronological timeline of KYC events from the applicant data.
        /// Timeline entries include application creation, review initiation,
        /// and verification status changes.
        /// </summary>
        private void BuildTimeline(SumsubApplicant applicant)
        {
            TimelineLayout.Clear();
            var timelineItems = BuildTimelineEvents(applicant);

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
                    gap = "Latest";
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

        private List<TimelineEventItem> BuildTimelineEvents(SumsubApplicant applicant)
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

            return timelineItems;
        }

        private static DateTime? ParseSumsubDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
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
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                }
            };

            var leftColumn = new VerticalStackLayout
            {
                WidthRequest = 56,
                Spacing = 2,
                VerticalOptions = LayoutOptions.Fill
            };

            leftColumn.Add(new Label
            {
                Text = timeGapText,
                FontSize = 11,
                TextColor = Color.FromArgb("#6E6E6E"),
                HorizontalTextAlignment = TextAlignment.Start,
                IsVisible = !string.IsNullOrWhiteSpace(timeGapText)
            });

            var markerGrid = new Grid
            {
                WidthRequest = 20,
                HeightRequest = 88,
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

            leftColumn.Add(markerGrid);
            wrapper.Add(leftColumn, 0, 0);

            var content = new VerticalStackLayout { Spacing = 4 };
            var titleRow = new HorizontalStackLayout { Spacing = 8 };

            titleRow.Add(new Label
            {
                Text = item.Action,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.StartAndExpand
            });

            titleRow.Add(new Label
            {
                Text = item.Status.ToUpperInvariant(),
                FontSize = 10,
                Padding = new Thickness(8, 2),
                BackgroundColor = dotColor.WithAlpha(0.2f),
                TextColor = dotColor,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End
            });

            content.Add(titleRow);

            if (item.Date.HasValue)
            {
                content.Add(new Label
                {
                    Text = item.Date.Value.ToString("MMM dd, yyyy HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6E6E6E")
                });
            }

            if (!string.IsNullOrWhiteSpace(item.Subtitle))
            {
                content.Add(new Label
                {
                    Text = item.Subtitle,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6E6E6E")
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
                    TextColor = Color.FromArgb("#6E6E6E")
                });
            }

            var eventCard = new Card
            {
                CardPadding = new Thickness(12, 10),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Start
            };

            eventCard.View = content;
            wrapper.Add(eventCard, 1, 0);

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
    }
}
