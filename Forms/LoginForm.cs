using System;
using System.Drawing;
using System.Windows.Forms;

namespace OrderApp.Forms
{
    public partial class LoginForm : Form
    {
        public string? Username { get; private set; }

        private TextBox? usernameTextBox;
        private TextBox? passwordTextBox;
        private Button? loginButton;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ---- Critical fix ----
            // The form's design was laid out for a 96 DPI (100% scaling) screen.
            // Without telling WinForms that explicitly, Windows scales the FONTS
            // one way and the hard-coded pixel Locations another way on a
            // different-DPI monitor (common on larger/higher-res PCs), which is
            // exactly what pushed the "Username:"/"Password:" labels out of
            // alignment with their textboxes in your screenshot.
            // Declaring AutoScaleMode.Dpi + AutoScaleDimensions makes WinForms
            // scale every control (position, size, and font) by the same
            // uniform factor, so the layout you see at design time is what you
            // get on any screen.
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            // Form settings
            this.Text = "Login - POS SYSTEM";
            this.ClientSize = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 52, 54);

            // Title with icon
            var titleLabel = new Label
            {
                Location = new Point(125, 25),
                Size = new Size(200, 50),
                Text = "🛒 POS SYSTEM",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 152, 219),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Info text
            var infoLabel = new Label
            {
                Location = new Point(50, 75),
                Size = new Size(350, 40),
                Text = "ℹ️  Please enter your username correctly.\nThis will be displayed as the operator on receipts.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(149, 165, 166),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Username label
            var usernameLabel = new Label
            {
                Location = new Point(50, 130),
                Size = new Size(100, 25),
                Text = "Username:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White
            };

            // Username textbox - anchored so it stretches a little if the
            // form is ever resized, but stays aligned to the label
            usernameTextBox = new TextBox
            {
                Location = new Point(150, 128),
                Size = new Size(220, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Enter your username"
            };

            // Password label
            var passwordLabel = new Label
            {
                Location = new Point(50, 175),
                Size = new Size(100, 25),
                Text = "Password:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White
            };

            // Password textbox
            passwordTextBox = new TextBox
            {
                Location = new Point(150, 173),
                Size = new Size(220, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '•',
                PlaceholderText = "Enter your password"
            };

            // Login button with hover effect
            loginButton = new Button
            {
                Location = new Point(150, 230),
                Size = new Size(150, 40),
                Anchor = AnchorStyles.Top,
                Text = "🔐 Login",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            loginButton.FlatAppearance.BorderSize = 0;
            loginButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
            loginButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(31, 120, 175);
            loginButton.Click += LoginButton_Click;

            // Add controls
            this.Controls.AddRange(new Control[] { titleLabel, infoLabel, usernameLabel, usernameTextBox, passwordLabel, passwordTextBox, loginButton });

            // Set default button
            this.AcceptButton = loginButton;

            this.ResumeLayout(false);
        }

        private void LoginButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(usernameTextBox?.Text))
            {
                MessageBox.Show("Please enter username", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordTextBox?.Text))
            {
                MessageBox.Show("Please enter password", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (passwordTextBox.Text != "123456")
            {
                MessageBox.Show("Invalid password. Please Try again", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                passwordTextBox.Clear();
                passwordTextBox.Focus();
                return;
            }

            Username = usernameTextBox.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}