using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DogVCatClassifier.Functionality;

namespace DogVCatClassifier
{
    public partial class MainForm : Form
    {

        //Variables for UI Components
        private Panel screenUpload;
        private Panel screenLoading;
        private Panel screenResults;

        private Button v2ModelButton;
        private Button v3ModelButton;

        //Loading animation
        private System.Windows.Forms.Timer spinnerTimer;
        private PictureBox spinnerImage;
        private float spinnerAngle = 0;
        private Label loadingTextLabel;

        private Panel uploadArea;
        private Label uploadIcon;
        private Label uploadText;
        private Button uploadButton;

        private PictureBox previewImage;
        private Label imageLabel;
        private Label modelInfoLabel;
        private Label resultHeading;
        private Panel classificationResult;
        private Label animalIcon;
        private Label animalName;
        private Panel confidenceBarContainer;
        private Panel confidenceFill;
        private Label confidencePercentage;
        private Panel confidenceTextPanel;
        private Label lowConfidenceLabel;
        private Label highConfidenceLabel;

        private Panel analysisDetails;
        private TableLayoutPanel detailsTable;
        private Button retryButton;
        
        private Panel uncertainWarningPanel;
        private Label uncertainWarningLabel;

        private OpenFileDialog openFileDialog;
        
        // Dark mode
        private bool isDarkMode = false;
        private Button darkModeButton;
        private Label headerTitle;
        private Label headerSubtitle;
        private Panel mainContainer;

        //Classifier Service
        private Functionality.PetClassifier classifierService;

        public MainForm()
        {
            InitializeComponent();

            //Setting up the main window
            this.Size = new Size(1262, 673);
            this.BackColor = ColorTranslator.FromHtml("#f5f7fa");
            this.Font = new Font("Segoe UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            classifierService = new Functionality.PetClassifier();

            SetupUI();

        }

        //Setting up the user interface
        private void SetupUI()
        {
            // Dark mode toggle button (top right)
            darkModeButton = new Button
            {
                Text = "🌙",
                Font = new Font("Segoe UI", 14),
                Size = new Size(45, 45),
                Location = new Point(this.ClientSize.Width - 65, 15),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#eef1f5"),
                ForeColor = ColorTranslator.FromHtml("#333"),
                Cursor = Cursors.Hand
            };
            darkModeButton.FlatAppearance.BorderSize = 0;
            darkModeButton.Click += DarkModeButton_Click;
            this.Controls.Add(darkModeButton);

            //header title
            headerTitle = new Label
            {
                Text = "Pet Classifier",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#4a6fa5"),
                AutoSize = true
            };
            headerTitle.Location = new Point((this.ClientSize.Width - headerTitle.PreferredWidth) / 2, 20);
            this.Controls.Add(headerTitle);

            //subtitle
            headerSubtitle = new Label
            {
                Text = "Upload an image to identify if it's a cat or a dog",
                Font = new Font("Segoe UI", 11),
                ForeColor = ColorTranslator.FromHtml("#666"),
                AutoSize = true
            };
            headerSubtitle.Location = new Point((this.ClientSize.Width - headerSubtitle.PreferredWidth) / 2, headerTitle.Bottom + 5);
            this.Controls.Add(headerSubtitle);

            //Main container
            mainContainer = new Panel
            {

                BackColor = Color.White,
                Size = new Size(900, 550),
                Location = new Point((this.ClientSize.Width - 900) / 2, headerSubtitle.Bottom + 20),
                BorderStyle = BorderStyle.None

            };

            //added rounded corners to the main container
            mainContainer.Region = new Region(RoundedRect(mainContainer.ClientRectangle, 12));
            this.Controls.Add(mainContainer);


            //model selector panel
            Panel modelSelectorPanel = new Panel
            {
                Size = new Size(250, 40),
                Location = new Point((mainContainer.Width - 250) / 2 , 20),
                BackColor = Color.Transparent
            };
            mainContainer.Controls.Add(modelSelectorPanel);

            //button for selecting models
            //v2
            v2ModelButton = new Button
            {
                Text = "Model v2",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(125, 40),
                Location = new Point(0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#4a6fa5"),
                ForeColor = Color.White,
                TabStop = false
            };
            v2ModelButton.FlatAppearance.BorderSize = 0;
            v2ModelButton.Click += V2ModelButton_Click;
            modelSelectorPanel.Controls.Add(v2ModelButton);

            //v3
            v3ModelButton = new Button
            {
                Text = "Model v3",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(125, 40),
                Location = new Point(125, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#eef1f5"),
                ForeColor = ColorTranslator.FromHtml("#333"),
                TabStop = false
            };
            v3ModelButton.FlatAppearance.BorderSize = 0;
            v3ModelButton.Click += V3ModelButton_Click;
            modelSelectorPanel.Controls.Add(v3ModelButton);

            //create the different screens of the application
            CreateUploadScreen(mainContainer);
            CreateLoadingScreen(mainContainer);
            CreateResultsScreen(mainContainer);

            //for selecting image files
            openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Select an image"
            };

            ShowScreen("upload");
        }



        //UI Elements
        private void CreateUploadScreen(Panel container)
        {
            //for centering all elements in the panel
            int centerY = 50;

            //main panel
            screenUpload = new Panel
            {
                Size = new Size(container.Width - 40, 400),
                Location = new Point(20, 80),
                BackColor = Color.Transparent,
                Visible = true
            };
            container.Controls.Add(screenUpload);

            //drag and drop upload area
            uploadArea = new Panel
            {
                Size = new Size(screenUpload.Width - 40, 300),
                Location = new Point(20, 20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            //rounded corners on the upload area
            uploadArea.Region = new Region(RoundedRect(new Rectangle(0, 0, uploadArea.Width, uploadArea.Height), 8));
            
            // Custom paint for rounded border
            uploadArea.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = RoundedRect(new Rectangle(1, 1, uploadArea.Width - 3, uploadArea.Height - 3), 8))
                using (var pen = new Pen(isDarkMode ? ColorTranslator.FromHtml("#555") : ColorTranslator.FromHtml("#ccd7e6"), 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            screenUpload.Controls.Add(uploadArea);

            uploadIcon = new Label
            {
                Text = "📁",
                Font = new Font("Segoe UI", 36),
                ForeColor = ColorTranslator.FromHtml("#4a6fa5"),
                AutoSize = true
            };
            uploadArea.Controls.Add(uploadIcon);
            uploadIcon.Location = new Point((uploadArea.Width - uploadIcon.Width) / 2, centerY);

            uploadText = new Label
            {
                Text = "Drag & drop an image here or click to browse",
                Font = new Font("Segoe UI", 11),
                ForeColor = ColorTranslator.FromHtml("#666"),
                AutoSize = true
            };
            uploadArea.Controls.Add(uploadText);
            uploadText.Location = new Point((uploadArea.Width - uploadText.Width) / 2, uploadIcon.Bottom + 20);

            uploadButton = new Button
            {
                Text = "Choose Image",
                Font = new Font("Segoe UI", 10),
                Size = new Size(150, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#4a6fa5"),
                ForeColor = Color.White
            };
            uploadButton.FlatAppearance.BorderSize = 0;
            uploadButton.Click += UploadButton_Click;
            uploadArea.Controls.Add(uploadButton);
            uploadButton.Location = new Point((uploadArea.Width - uploadButton.Width) / 2, uploadText.Bottom + 20);

            uploadArea.AllowDrop = true;
            uploadArea.DragEnter += UploadArea_DragEnter;
            uploadArea.DragDrop += UploadArea_DragDrop;

        }

        //Loading Screen
        private void CreateLoadingScreen(Panel container)
        {
            //Main panel
            screenLoading = new Panel
            {
                Size = new Size(container.Width - 40, 400),
                Location = new Point(20, 80),
                BackColor = Color.Transparent,
                Visible = false
            };
            container.Controls.Add(screenLoading);

            //spinner animation
            spinnerImage = new PictureBox
            {
                Size = new Size(50, 50),
                Location = new Point((screenLoading.Width - 50) / 2, 140),
                Image = CreateSpinnerImage(),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent
            };
            screenLoading.Controls.Add(spinnerImage);

            loadingTextLabel = new Label
            {
                Text = "Analyzing image with Model v2...",
                Font = new Font("Segoe UI", 11),
                ForeColor = ColorTranslator.FromHtml("#4a6fa5"),
                AutoSize = true
            };
            loadingTextLabel.Location = new Point((screenLoading.Width - loadingTextLabel.PreferredWidth) / 2, spinnerImage.Bottom + 25);
            screenLoading.Controls.Add(loadingTextLabel);

            //Setup spinner animation timer
            spinnerTimer = new System.Windows.Forms.Timer();
            spinnerTimer.Interval = 50; //20 FPS
            spinnerTimer.Tick += SpinnerTimer_Tick;
        }

        private void SpinnerTimer_Tick(object sender, EventArgs e)
        {
            spinnerAngle += 15;
            if (spinnerAngle >= 360) spinnerAngle = 0;
            
            spinnerImage.Image?.Dispose();
            spinnerImage.Image = CreateRotatedSpinnerImage(spinnerAngle);
        }

        private Image CreateRotatedSpinnerImage(float angle)
        {
            Bitmap bitmap = new Bitmap(50, 50);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);
                
                //Translate to center, rotate, then translate back
                graphics.TranslateTransform(25, 25);
                graphics.RotateTransform(angle);
                graphics.TranslateTransform(-25, -25);

                //Draw the arc spinner
                using (Pen activePen = new Pen(ColorTranslator.FromHtml("#4a6fa5"), 4))
                {
                    graphics.DrawArc(activePen, 5, 5, 40, 40, 0, 270);
                }

                using (Pen inactivePen = new Pen(ColorTranslator.FromHtml("#dce3ed"), 4))
                {
                    graphics.DrawArc(inactivePen, 5, 5, 40, 40, 270, 90);
                }
            }
            return bitmap;
        }

        //Results Screen
        private void CreateResultsScreen(Panel container)
        {
            //Main panel
            screenResults = new Panel
            {
                Size = new Size(container.Width - 40, 400),
                Location = new Point(20, 80),
                BackColor = Color.Transparent,
                Visible = false
            };
            container.Controls.Add(screenResults);

            //panel for image preview
            Panel leftPanel = new Panel
            {
                Size = new Size(300, 400),
                Location = new Point(0, 0),
                BackColor = Color.Transparent
            };
            screenResults.Controls.Add(leftPanel);

            Panel imagePreviewPanel = new Panel
            {
                Size = new Size(280, 280),
                Location = new Point(10, 10),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            imagePreviewPanel.Region = new Region(RoundedRect(imagePreviewPanel.ClientRectangle, 8));

            //custom paint handler to draw a border
            imagePreviewPanel.Paint += (s, e) =>
            {
                Rectangle rectangle = new Rectangle(0, 0, imagePreviewPanel.Width - 1, imagePreviewPanel.Height - 1);

                using (Pen pen = new Pen(ColorTranslator.FromHtml("#ccd7e6"), 1))
                {
                    e.Graphics.DrawRectangle(pen, rectangle);

                }
            };
            leftPanel.Controls.Add(imagePreviewPanel);

            //Label at bottom of image preview - add this FIRST so it's behind the image
            Panel imageLabelPanel = new Panel
            {
                Size = new Size(280, 30),
                Location = new Point(0, 248),
                BackColor = Color.FromArgb(180, 0, 0, 0)
            };
            imagePreviewPanel.Controls.Add(imageLabelPanel);

            imageLabel = new Label
            {
                Text = "Uploaded image",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 5),
                Size = new Size(280, 25)
            };
            imageLabelPanel.Controls.Add(imageLabel);

            //display the actual image - add AFTER label panel so it's on top
            previewImage = new PictureBox
            {
                Size = new Size(260, 240),
                Location = new Point(10, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            imagePreviewPanel.Controls.Add(previewImage);

            //classification results panel
            Panel rightPanel = new Panel
            {
                Size = new Size(screenResults.Width - leftPanel.Width, 400),
                Location = new Point(leftPanel.Width, 0),
                BackColor = Color.Transparent
            };
            screenResults.Controls.Add(rightPanel);

            Panel modelInfoPanel = new Panel
            {
                Size = new Size(350, 30),
                Location = new Point(20, 10),
                BackColor = ColorTranslator.FromHtml("#f0f4f8")
            };
            modelInfoPanel.Region = new Region(RoundedRect(modelInfoPanel.ClientRectangle, 6));
            rightPanel.Controls.Add(modelInfoPanel);

            modelInfoLabel = new Label
            {
                Text = "🔍 Currently using: Model v2",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorTranslator.FromHtml("#4a6fa5"),
                Location = new Point(10, 7),
                AutoSize = true
            };
            modelInfoPanel.Controls.Add(modelInfoLabel);
            
            // Retry button - positioned at top right next to model info
            retryButton = new Button
            {
                Text = "🔄 New Image",
                Font = new Font("Segoe UI", 9),
                Size = new Size(120, 30),
                Location = new Point(modelInfoPanel.Right + 10, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#4a6fa5"),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            retryButton.FlatAppearance.BorderSize = 0;
            retryButton.Click += RetryButton_Click;
            rightPanel.Controls.Add(retryButton);

            resultHeading = new Label
            {
                Text = "Classification Result",
                Font = new Font("Segoe UI", 14),
                ForeColor = ColorTranslator.FromHtml("#4a6fa5"),
                Location = new Point(20, modelInfoPanel.Bottom + 15),
                AutoSize = true
            };
            rightPanel.Controls.Add(resultHeading);

            classificationResult = new Panel
            {
                Size = new Size(rightPanel.Width - 40, 110),
                Location = new Point(20, resultHeading.Bottom + 15),
                BackColor = ColorTranslator.FromHtml("#f8fafc")
            };
            classificationResult.Region = new Region(RoundedRect(classificationResult.ClientRectangle, 8));

            //adding a colored border
            classificationResult.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(
                    
                    new Pen(ColorTranslator.FromHtml("#4a6fa5"), 4),
                    new Point(0, 0),
                    new Point(0, classificationResult.Height)

                );
            };
            rightPanel.Controls.Add(classificationResult);

            animalIcon = new Label
            {
                Text = "🐶", //default icon (dog)
                Font = new Font("Segoe UI", 24),
                Location = new Point(15, 10),
                AutoSize = true
            };
            classificationResult.Controls.Add(animalIcon);

            animalName = new Label
            {
                Text = "Dog", 
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(animalIcon.Right + 10, 15),
                AutoSize = true
            };
            classificationResult.Controls.Add(animalName);


            confidenceBarContainer = new Panel
            {
                Size = new Size(classificationResult.Width - animalIcon.Right - 30, 24),
                Location = new Point(animalIcon.Right + 10, animalName.Bottom + 5),
                BackColor = ColorTranslator.FromHtml("#eef1f5")
            };
            confidenceBarContainer.Region = new Region(RoundedRect(confidenceBarContainer.ClientRectangle, 12));
            classificationResult.Controls.Add(confidenceBarContainer);

            confidenceFill = new Panel
            {
                Size = new Size((int)(confidenceBarContainer.Width * 0.92), 24),  
                Location = new Point(0, 0),
                BackColor = ColorTranslator.FromHtml("#4CAF50")
            };
            confidenceFill.Region = new Region(RoundedRect(confidenceFill.ClientRectangle, 12));
            confidenceBarContainer.Controls.Add(confidenceFill);

            confidencePercentage = new Label
            {
                Text = "92%",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(confidenceFill.Width - 30, 5)
            };
            confidenceFill.Controls.Add(confidencePercentage);

            confidenceTextPanel = new Panel
            {
                Size = new Size(confidenceBarContainer.Width, 20),
                Location = new Point(animalIcon.Right + 10, confidenceBarContainer.Bottom + 5),
                BackColor = Color.Transparent
            };
            classificationResult.Controls.Add(confidenceTextPanel);

            lowConfidenceLabel = new Label
            {
                Text = "Low confidence",
                Font = new Font("Segoe UI", 8),
                ForeColor = ColorTranslator.FromHtml("#666"),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            confidenceTextPanel.Controls.Add(lowConfidenceLabel);

            highConfidenceLabel = new Label
            {
                Text = "High confidence",
                Font = new Font("Segoe UI", 8),
                ForeColor = ColorTranslator.FromHtml("#666"),
                AutoSize = true,
                Location = new Point(confidenceTextPanel.Width - 100, 0)
            };
            confidenceTextPanel.Controls.Add(highConfidenceLabel);

            // Warning panel for uncertain classifications (non-pet images)
            uncertainWarningPanel = new Panel
            {
                Size = new Size(rightPanel.Width - 40, 35),
                Location = new Point(20, classificationResult.Bottom + 8),
                BackColor = ColorTranslator.FromHtml("#fff3cd"),
                Visible = false
            };
            uncertainWarningPanel.Region = new Region(RoundedRect(uncertainWarningPanel.ClientRectangle, 6));
            
            // Add orange left border
            uncertainWarningPanel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(
                    new Pen(ColorTranslator.FromHtml("#ff9800"), 4),
                    new Point(0, 0),
                    new Point(0, uncertainWarningPanel.Height)
                );
            };
            rightPanel.Controls.Add(uncertainWarningPanel);
            
            uncertainWarningLabel = new Label
            {
                Text = "⚠️ Low confidence - This image may not be a cat or dog",
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorTranslator.FromHtml("#856404"),
                AutoSize = false,
                Size = new Size(uncertainWarningPanel.Width - 20, 25),
                Location = new Point(10, 5),
                TextAlign = ContentAlignment.MiddleLeft
            };
            uncertainWarningPanel.Controls.Add(uncertainWarningLabel);
            
            analysisDetails = new Panel
            {
                Size = new Size(rightPanel.Width - 40, 120),
                Location = new Point(20, classificationResult.Bottom + 15),
                BackColor = ColorTranslator.FromHtml("#f8fafc")
            };
            analysisDetails.Region = new Region(RoundedRect(analysisDetails.ClientRectangle, 8));
            rightPanel.Controls.Add(analysisDetails);

            detailsTable = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                Size = new Size(analysisDetails.Width - 20, 110),
                Location = new Point(10, 5),
                BackColor = Color.Transparent
            };
            detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            detailsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            analysisDetails.Controls.Add(detailsTable);

            //placeholders
            AddDetailRow("Cat probability:", "8%");
            AddDetailRow("Dog probability:", "92%");
            AddDetailRow("Processing time:", "0.24 seconds");
            AddDetailRow("Image size:", "512x384 pixels");

        }




        //UI Methods

        private Image CreateSpinnerImage()
        {
            Bitmap bitmap = new Bitmap(50, 50);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {   
                //enable anti aliasing to smooth edges
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                using (Pen pen = new Pen(ColorTranslator.FromHtml("#4a6fa5"), 4))
                {
                    graphics.DrawArc(pen, 5, 5, 40, 40, 0, 270);
                }

                using (Pen pen = new Pen(ColorTranslator.FromHtml("#dce3ed"), 4))
                {
                    graphics.DrawArc(pen, 5, 5, 40, 40, 270, 90);
                }
            }

            return bitmap;
        }

        //creates a rounded rectangle
        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            //calculate diameter for the corner arcs
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            //top left
            path.AddArc(arc, 180, 90);

            //top right
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            //bottom right 
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            //bottom left 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        private void ShowScreen(string screenName)
        {
            screenUpload.Visible = (screenName == "upload");
            screenLoading.Visible = (screenName == "loading");
            screenResults.Visible = (screenName == "results");

            //Control spinner animation
            if (screenName == "loading")
            {
                spinnerTimer?.Start();
            }
            else
            {
                spinnerTimer?.Stop();
            }

            //Enable/disable model buttons - only allow switching on upload screen
            bool canSwitchModels = (screenName == "upload");
            v2ModelButton.Enabled = canSwitchModels;
            v3ModelButton.Enabled = canSwitchModels;
            
            // Theme-aware colors
            Color accentColor = isDarkMode ? ColorTranslator.FromHtml("#4a90d9") : ColorTranslator.FromHtml("#4a6fa5");
            Color buttonInactiveBg = isDarkMode ? ColorTranslator.FromHtml("#333333") : ColorTranslator.FromHtml("#eef1f5");
            Color textPrimary = isDarkMode ? ColorTranslator.FromHtml("#e8e8e8") : ColorTranslator.FromHtml("#333");
            Color disabledBg = isDarkMode ? ColorTranslator.FromHtml("#2a2a2a") : ColorTranslator.FromHtml("#ccd7e6");
            Color disabledText = isDarkMode ? ColorTranslator.FromHtml("#666") : ColorTranslator.FromHtml("#888");
            
            //Visual feedback for disabled state
            if (!canSwitchModels)
            {
                v2ModelButton.BackColor = disabledBg;
                v3ModelButton.BackColor = disabledBg;
                v2ModelButton.ForeColor = disabledText;
                v3ModelButton.ForeColor = disabledText;
            }
            else
            {
                //Restore active model button colors
                if (classifierService.GetCurrentModel() == "v2")
                {
                    v2ModelButton.BackColor = accentColor;
                    v2ModelButton.ForeColor = Color.White;
                    v3ModelButton.BackColor = buttonInactiveBg;
                    v3ModelButton.ForeColor = textPrimary;
                }
                else
                {
                    v3ModelButton.BackColor = accentColor;
                    v3ModelButton.ForeColor = Color.White;
                    v2ModelButton.BackColor = buttonInactiveBg;
                    v2ModelButton.ForeColor = textPrimary;
                }
            }
        }


        //Event Methods

        private void UploadArea_DragEnter(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                uploadArea.BackColor = isDarkMode ? 
                    Color.FromArgb(30, 74, 144, 217) : 
                    Color.FromArgb(10, 74, 111, 165);
            }
        }

        private void UploadArea_DragDrop(object sender, DragEventArgs e)
        {
            uploadArea.BackColor = isDarkMode ? ColorTranslator.FromHtml("#1e1e1e") : Color.White;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if(files.Length > 0 )
            {
                ProcessImage(files[0]);
            }
        }

        //Button Functionality

        private void V2ModelButton_Click(object sender, EventArgs e)
        {
            Color accentColor = isDarkMode ? ColorTranslator.FromHtml("#4a90d9") : ColorTranslator.FromHtml("#4a6fa5");
            Color buttonInactiveBg = isDarkMode ? ColorTranslator.FromHtml("#333333") : ColorTranslator.FromHtml("#eef1f5");
            Color textPrimary = isDarkMode ? ColorTranslator.FromHtml("#e8e8e8") : ColorTranslator.FromHtml("#333");
            
            v2ModelButton.BackColor = accentColor;
            v2ModelButton.ForeColor = Color.White;
            v3ModelButton.BackColor = buttonInactiveBg;
            v3ModelButton.ForeColor = textPrimary;

            modelInfoLabel.Text = "🔍 Currently using: Model v2";
            loadingTextLabel.Text = "Analyzing image with Model v2...";

            classifierService.SetModel("v2");
        }

        private void V3ModelButton_Click(object sender, EventArgs e)
        {
            Color accentColor = isDarkMode ? ColorTranslator.FromHtml("#4a90d9") : ColorTranslator.FromHtml("#4a6fa5");
            Color buttonInactiveBg = isDarkMode ? ColorTranslator.FromHtml("#333333") : ColorTranslator.FromHtml("#eef1f5");
            Color textPrimary = isDarkMode ? ColorTranslator.FromHtml("#e8e8e8") : ColorTranslator.FromHtml("#333");
            
            v3ModelButton.BackColor = accentColor;
            v3ModelButton.ForeColor = Color.White;
            v2ModelButton.BackColor = buttonInactiveBg;
            v2ModelButton.ForeColor = textPrimary;

            modelInfoLabel.Text = "🔍 Currently using: Model v3";
            loadingTextLabel.Text = "Analyzing image with Model v3...";

            classifierService.SetModel("v3");
        }

        private void UploadButton_Click(object? sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ProcessImage(openFileDialog.FileName);
            }
        }

        private void RetryButton_Click(object sender, EventArgs e)
        {
            ShowScreen("upload");
        }

        private void DarkModeButton_Click(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            // Define theme colors - uniform background for dark mode
            Color formBg = isDarkMode ? ColorTranslator.FromHtml("#1e1e1e") : ColorTranslator.FromHtml("#f5f7fa");
            Color containerBg = isDarkMode ? ColorTranslator.FromHtml("#1e1e1e") : Color.White;
            Color cardBg = isDarkMode ? ColorTranslator.FromHtml("#1e1e1e") : ColorTranslator.FromHtml("#f8fafc");
            Color accentColor = isDarkMode ? ColorTranslator.FromHtml("#4a90d9") : ColorTranslator.FromHtml("#4a6fa5");
            Color textPrimary = isDarkMode ? ColorTranslator.FromHtml("#e8e8e8") : ColorTranslator.FromHtml("#333");
            Color textSecondary = isDarkMode ? ColorTranslator.FromHtml("#a0a0a0") : ColorTranslator.FromHtml("#666");
            Color buttonInactiveBg = isDarkMode ? ColorTranslator.FromHtml("#333333") : ColorTranslator.FromHtml("#eef1f5");
            Color infoPanelBg = isDarkMode ? ColorTranslator.FromHtml("#333333") : ColorTranslator.FromHtml("#f0f4f8");
            
            // Update dark mode button
            darkModeButton.Text = isDarkMode ? "☀️" : "🌙";
            darkModeButton.BackColor = buttonInactiveBg;
            darkModeButton.ForeColor = textPrimary;
            
            // Form background
            this.BackColor = formBg;
            
            // Header
            headerTitle.ForeColor = accentColor;
            headerSubtitle.ForeColor = textSecondary;
            
            // Main container
            mainContainer.BackColor = containerBg;
            
            // Upload area
            uploadArea.BackColor = containerBg;
            uploadIcon.ForeColor = accentColor;
            uploadText.ForeColor = textSecondary;
            uploadButton.BackColor = accentColor;
            uploadArea.Invalidate(); // Repaint border
            
            // Model selector buttons
            if (classifierService.GetCurrentModel() == "v2")
            {
                v2ModelButton.BackColor = accentColor;
                v2ModelButton.ForeColor = Color.White;
                v3ModelButton.BackColor = buttonInactiveBg;
                v3ModelButton.ForeColor = textPrimary;
            }
            else
            {
                v3ModelButton.BackColor = accentColor;
                v3ModelButton.ForeColor = Color.White;
                v2ModelButton.BackColor = buttonInactiveBg;
                v2ModelButton.ForeColor = textPrimary;
            }
            
            // Loading screen
            loadingTextLabel.ForeColor = accentColor;
            
            // Results screen elements
            resultHeading.ForeColor = accentColor;
            modelInfoLabel.ForeColor = accentColor;
            modelInfoLabel.Parent.BackColor = infoPanelBg;
            
            // Retry button
            retryButton.BackColor = accentColor;
            
            // Classification result panel
            classificationResult.BackColor = cardBg;
            animalName.ForeColor = textPrimary;
            confidenceBarContainer.BackColor = buttonInactiveBg;
            lowConfidenceLabel.ForeColor = textSecondary;
            highConfidenceLabel.ForeColor = textSecondary;
            
            // Analysis details
            analysisDetails.BackColor = cardBg;
            foreach (Control ctrl in detailsTable.Controls)
            {
                if (ctrl is Label lbl)
                {
                    if (lbl.Font.Bold)
                        lbl.ForeColor = textPrimary;
                    else
                        lbl.ForeColor = textSecondary;
                }
            }
            
            // Refresh panels
            classificationResult.Invalidate();
            screenUpload.Invalidate();
            screenResults.Invalidate();
        }

        //Other methods

        //adds a row to the details table
        private void AddDetailRow(string label, string value)
        {
            int rowIndex = detailsTable.Controls.Count / 2;

            Label labelControl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9),
                ForeColor = ColorTranslator.FromHtml("#666"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailsTable.Controls.Add(labelControl, 0, rowIndex);

            Label valueControl = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
            detailsTable.Controls.Add(valueControl, 1, rowIndex);
        }

        private void ProcessImage(string imagePath)
        {
            try
            {
                //Dispose previous image if any
                if (previewImage.Image != null)
                {
                    previewImage.Image.Dispose();
                    previewImage.Image = null;
                }

                //loading the image from the file and display it in the preview
                //Use a memory stream to avoid file locking
                using (var fs = new System.IO.FileStream(imagePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    Image img = Image.FromStream(fs);
                    previewImage.Image = new Bitmap(img);
                    
                    //get the image dimensions
                    Size imgSize = img.Size;
                    UpdateDetailValue("Image size:", $"{imgSize.Width}x{imgSize.Height} pixels");
                    
                    img.Dispose();
                }
                
                //Force refresh
                previewImage.Refresh();

                ShowScreen("loading");

                //start processing in the background
                System.Threading.ThreadPool.QueueUserWorkItem(new System.Threading.WaitCallback((o) =>
                {
                   
                    var startTime = DateTime.Now;
                    var result = classifierService.ClassifyImage(imagePath);
                    var processingTime = (DateTime.Now - startTime).TotalSeconds;

                    
                    this.Invoke(new Action(() =>
                    {
                        UpdateResults(result, processingTime);
                        ShowScreen("results");
                    }));
                }));
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error processing image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShowScreen("upload");
            }

        }

        private void UpdateResults(ClassificationResults result, double processingTime)
        {
            //update the results based on the classification
            if (result.IsUncertain)
            {
                // Show uncertain/unknown state
                animalIcon.Text = "❓";
                animalName.Text = "Uncertain";
                uncertainWarningPanel.Visible = true;
                
                // Adjust layout to make room for warning
                analysisDetails.Location = new Point(analysisDetails.Location.X, uncertainWarningPanel.Bottom + 8);
            }
            else
            {
                animalIcon.Text = result.IsDog ? "🐶" : "🐱";
                animalName.Text = result.IsDog ? "Dog" : "Cat";
                uncertainWarningPanel.Visible = false;
                
                // Reset layout position
                analysisDetails.Location = new Point(analysisDetails.Location.X, classificationResult.Bottom + 15);
            }

            //update the confidence
            int confidence = (int)(result.Confidence * 100);
            confidenceFill.Width = (int)(confidenceBarContainer.Width * result.Confidence);
            confidencePercentage.Text = $"{confidence}%";
            confidencePercentage.Location = new Point(Math.Max(confidenceFill.Width - 30, 5), 5);

            //update the confidence bar color based on confidence and uncertainty
            if (result.IsUncertain)
            {
                confidenceFill.BackColor = ColorTranslator.FromHtml("#f44336"); //red for uncertain
            }
            else
            {
                confidenceFill.BackColor = confidence > 70 ?
                    ColorTranslator.FromHtml("#4caf50") : //green for high
                    ColorTranslator.FromHtml("#ff9800"); //orange for low
            }

            //updating the details table
            UpdateDetailValue("Cat probability:", $"{(result.IsDog ? 100 - confidence : confidence)}%");
            UpdateDetailValue("Dog probability:", $"{(result.IsDog ? confidence : 100 - confidence)}%");
            UpdateDetailValue("Processing time:", $"{processingTime:F2} seconds");
        }

        private void UpdateDetailValue(string label, string value)
        {
            //search each row in the details table for the matching label
            for (int i = 0; i < detailsTable.RowCount; i++)
            {
                Label labelControl = detailsTable.GetControlFromPosition(0, i) as Label;
                if (labelControl != null && labelControl.Text == label)
                {
                    //found the matching row update its value
                    Label valueControl = detailsTable.GetControlFromPosition(1, i) as Label;
                    if (valueControl != null)
                    {
                        valueControl.Text = value;
                        return;
                    }
                }
            }
        }
    }


    //extension methods for Graphics

    public static class GraphicExtensions
    {
        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException("graphics");
            if (pen == null)
                throw new ArgumentNullException("pen");

            using (GraphicsPath path = RoundedRect(bounds, cornerRadius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            //calculate diameter for the corner arcs
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            //top left
            path.AddArc(arc, 180, 90);

            //top right
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            //bottom right 
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            //bottom left 
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}