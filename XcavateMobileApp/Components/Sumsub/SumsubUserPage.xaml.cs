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

                PopulateUserInfo(applicant, substrateKey);
                ShowStatusComponent(status);
                BuildTimeline(applicant);

                UserInfoCard.IsVisible = true;
                if (StatusComponentLayout.IsVisible)
                    StatusComponentLayout.IsVisible = true;
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

        private void ShowStatusComponent(SumsubStatusData status)
        {
            StatusComponentLayout.IsVisible = true;
            StatusLabel.Text = status.StatusType switch
            {
                SumsubStatusType.Approved => "Verification Approved",
                SumsubStatusType.Rejected => "Verification Rejected",
                SumsubStatusType.NeedsResubmit => "Needs Resubmission",
                SumsubStatusType.Pending => "Verification Pending",
                _ => "Verification Not Reviewed"
            };

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
        private void BuildTimeline(SumsubApplicant applicant)
        {
            var timelineItems = new List<(DateTime? Date, string Action, string Status)>();

            // Application creation event
            if (!string.IsNullOrEmpty(applicant.CreatedAt))
            {
                var createdDate = applicant.CreatedAtDateTime;
                timelineItems.Add((createdDate, "Application created", "success"));
            }

            // Review events
            var review = applicant.Review;
            if (review != null)
            {
                if (!string.IsNullOrEmpty(review.CreateDate))
                {
                    try
                    {
                        var reviewDate = DateTime.ParseExact(
                            review.CreateDate,
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                        );
                        string statusCategory = review.ReviewStatus?.ToLowerInvariant() switch
                        {
                            "approved" => "success",
                            "rejected" => "error",
                            "pending" => "warning",
                            _ => "info"
                        };
                        timelineItems.Add((reviewDate, $"Review {review.ReviewStatus?.ToLowerInvariant() ?? "created"}", statusCategory));
                    }
                    catch
                    {
                        timelineItems.Add((null, $"Review {review.ReviewStatus ?? "created"} (date unavailable)", "info"));
                    }
                }

                // Attempt count
                if (review.AttemptCnt.HasValue && review.AttemptCnt > 0)
                {
                    timelineItems.Add((null, $"Verification attempts: {review.AttemptCnt}", "info"));
                }

                // Auto-check mode
                if (!string.IsNullOrEmpty(review.LevelAutoCheckMode))
                {
                    timelineItems.Add((null, $"Auto-check mode: {review.LevelAutoCheckMode}", "info"));
                }
            }

            // Sort by date (nulls go to the end)
            timelineItems.Sort((a, b) =>
            {
                if (a.Date.HasValue && b.Date.HasValue) return a.Date.Value.CompareTo(b.Date.Value);
                if (a.Date.HasValue) return -1;
                if (b.Date.HasValue) return 1;
                return 0;
            });

            // Build UI for each timeline entry
            foreach (var item in timelineItems)
            {
                var timelineEntry = new HorizontalStackLayout
                {
                    Spacing = 10,
                    VerticalOptions = LayoutOptions.Start
                };

                // Color-coded status dot
                var dotColor = item.Status switch
                {
                    "success" => Colors.Green,
                    "error" => Colors.Red,
                    "warning" => Colors.Orange,
                    _ => Colors.Gray
                };

                var dot = new Label
                {
                    Text = "●",
                    TextColor = dotColor,
                    FontSize = 16,
                    VerticalOptions = LayoutOptions.Center
                };

                var textStack = new VerticalStackLayout
                {
                    Spacing = 2
                };

                var actionLabel = new Label
                {
                    Text = item.Action,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14
                };

                if (item.Date.HasValue)
                {
                    var dateLabel = new Label
                    {
                        Text = item.Date.Value.ToString("MMM dd, yyyy HH:mm:ss UTC"),
                        FontSize = 12,
                        TextColor = Colors.Gray
                    };
                    textStack.Add(dateLabel);
                }

                textStack.Add(actionLabel);
                timelineEntry.Add(dot);
                timelineEntry.Add(textStack);

                TimelineLayout.Add(timelineEntry);
            }
        }
    }
}
