using PlutoFramework.Model.Sumsub;
using PlutoFramework.Templates.PageTemplate;
using System.Globalization;

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
                    Text = "\u25cf",
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
