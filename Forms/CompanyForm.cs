using OrderApp.Data;
using OrderApp.Models;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace OrderApp.Forms
{
    public partial class CompanyForm : Form
    {
        public Company Company { get; private set; }

        private TextBox? companyNameTextBox;
        private TextBox? tradeNameTextBox;
        private TextBox? tinNumberTextBox;
        private TextBox? vatNumberTextBox;
        private TextBox? addressLine1TextBox;
        private TextBox? addressLine2TextBox;
        private TextBox? cityTextBox;
        private TextBox? stateProvinceTextBox;
        private TextBox? countryTextBox;
        private TextBox? postalCodeTextBox;
        private TextBox? telephoneTextBox;
        private TextBox? mobileTextBox;
        private TextBox? emailTextBox;
        private TextBox? websiteTextBox;
        private TextBox? logoTextBox;
        private NumericUpDown? taxRateNumeric;
        private TextBox? currencyCodeTextBox;
        private CheckBox? isActiveCheckBox;
        private Button? saveButton;
        private Button? cancelButton;

        public CompanyForm(Company? company = null)
        {
            Company = company ?? new Company
            {
                IsActive = true,
                TaxRate = 15,
                CurrencyCode = "ETB"
            };

            InitializeComponent();
            LoadCompanyData();
        }

        private void LoadCompanyData()
        {
            if (Company.Id > 0)
            {
                companyNameTextBox.Text = Company.CompanyName;
                tradeNameTextBox.Text = Company.TradeName ?? "";
                tinNumberTextBox.Text = Company.TinNumber ?? "";
                vatNumberTextBox.Text = Company.VatNumber ?? "";
                addressLine1TextBox.Text = Company.AddressLine1 ?? "";
                addressLine2TextBox.Text = Company.AddressLine2 ?? "";
                cityTextBox.Text = Company.City ?? "";
                stateProvinceTextBox.Text = Company.StateProvince ?? "";
                countryTextBox.Text = Company.Country ?? "";
                postalCodeTextBox.Text = Company.PostalCode ?? "";
                telephoneTextBox.Text = Company.Telephone ?? "";
                mobileTextBox.Text = Company.Mobile ?? "";
                emailTextBox.Text = Company.Email ?? "";
                websiteTextBox.Text = Company.Website ?? "";
                logoTextBox.Text = Company.Logo ?? "";
                taxRateNumeric.Value = Company.TaxRate ?? 15;
                currencyCodeTextBox.Text = Company.CurrencyCode ?? "ETB";
                isActiveCheckBox.Checked = Company.IsActive;
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ---- Scaling: DPI-aware so the form doesn't distort on
            // small or high-DPI screens ----
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new SizeF(96F, 96F);

            this.Text = Company.Id > 0 ? "Edit Company" : "Add Company";
            this.ClientSize = new Size(700, 650);
            this.MinimumSize = new Size(560, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            // Resizable instead of FixedSingle so it can shrink/grow to fit the screen
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(240, 242, 245);

            // ---- Root: scrollable content area on top, fixed button bar pinned to the bottom ----
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Scrollable content panel - if the window is smaller than the form's
            // natural content height, a scrollbar appears instead of fields
            // getting clipped or squeezed.
            var scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(240, 242, 245)
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(20, 15, 20, 15)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // Company Name - full width
            var companyNameField = BuildField("Company Name:", out companyNameTextBox, bold: true);
            grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(companyNameField, 0, grid.RowCount - 1);
            grid.SetColumnSpan(companyNameField, 2);

            AddFieldRow(grid, "Trade Name:", out tradeNameTextBox, "TIN Number:", out tinNumberTextBox);
            AddFieldRow(grid, "VAT Number:", out vatNumberTextBox, "Address Line 1:", out addressLine1TextBox);
            AddFieldRow(grid, "Address Line 2:", out addressLine2TextBox, "City:", out cityTextBox);
            AddFieldRow(grid, "State/Province:", out stateProvinceTextBox, "Country:", out countryTextBox);
            AddFieldRow(grid, "Postal Code:", out postalCodeTextBox, "Telephone:", out telephoneTextBox);
            AddFieldRow(grid, "Mobile:", out mobileTextBox, "Email:", out emailTextBox);
            AddFieldRow(grid, "Website:", out websiteTextBox, "Logo URL:", out logoTextBox);

            // Tax Rate + Currency Code (numeric / short text, not full-width)
            taxRateNumeric = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 100,
                Height = 25,
                Minimum = 0,
                Maximum = 100,
                Value = 15,
                DecimalPlaces = 2,
                Font = new Font("Segoe UI", 9)
            };
            currencyCodeTextBox = new TextBox
            {
                Dock = DockStyle.Left,
                Width = 100,
                Height = 25,
                Font = new Font("Segoe UI", 9),
                Text = "ETB"
            };
            var taxRateField = WrapWithLabel("Tax Rate (%):", taxRateNumeric);
            var currencyField = WrapWithLabel("Currency Code:", currencyCodeTextBox);
            grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(taxRateField, 0, grid.RowCount - 1);
            grid.Controls.Add(currencyField, 1, grid.RowCount - 1);

            // Is Active
            isActiveCheckBox = new CheckBox
            {
                AutoSize = true,
                Text = "Active",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Checked = true,
                Margin = new Padding(3, 12, 3, 3)
            };
            grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(isActiveCheckBox, 0, grid.RowCount - 1);

            scrollHost.Controls.Add(grid);

            // Buttons panel at bottom - stays visible regardless of scroll position
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 60,
                BackColor = Color.FromArgb(230, 230, 230)
            };

            saveButton = new Button
            {
                Location = new Point(20, 12),
                Size = new Size(120, 36),
                Text = "💾 Save",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Location = new Point(150, 12),
                Size = new Size(120, 36),
                Text = "❌ Cancel",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            buttonPanel.Controls.AddRange(new Control[] { saveButton, cancelButton });

            var buttonRowHost = new Panel { Dock = DockStyle.Fill, Height = 60, AutoSize = false };
            buttonRowHost.Controls.Add(buttonPanel);

            root.Controls.Add(scrollHost, 0, 0);
            root.Controls.Add(buttonRowHost, 0, 1);

            this.Controls.Add(root);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Adds a row containing two label+textbox fields side by side.
        /// </summary>
        private void AddFieldRow(TableLayoutPanel grid, string label1, out TextBox box1, string label2, out TextBox box2)
        {
            var field1 = BuildField(label1, out box1);
            var field2 = BuildField(label2, out box2);
            grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(field1, 0, grid.RowCount - 1);
            grid.Controls.Add(field2, 1, grid.RowCount - 1);
        }

        /// <summary>
        /// Builds a stacked label + textbox that stretches to fill its column
        /// instead of relying on fixed pixel coordinates.
        /// </summary>
        private Panel BuildField(string labelText, out TextBox textBox, bool bold = true)
        {
            textBox = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9),
                Margin = new Padding(0, 2, 0, 0)
            };
            return WrapWithLabel(labelText, textBox, bold);
        }

        /// <summary>
        /// Wraps any control with a label above it, stretching horizontally with its parent.
        /// </summary>
        private Panel WrapWithLabel(string labelText, Control control, bool bold = true)
        {
            var label = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Text = labelText,
                Font = new Font("Segoe UI", 9, bold ? FontStyle.Bold : FontStyle.Regular)
            };

            control.Dock = DockStyle.Top;

            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(10, 5, 10, 5)
            };
            // Add in reverse order since Dock = Top stacks from the bottom up
            wrapper.Controls.Add(control);
            wrapper.Controls.Add(label);

            return wrapper;
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(companyNameTextBox?.Text))
            {
                MessageBox.Show("Company Name is required", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Company.CompanyName = companyNameTextBox.Text.Trim();
            Company.TradeName = tradeNameTextBox?.Text?.Trim();
            Company.TinNumber = tinNumberTextBox?.Text?.Trim();
            Company.VatNumber = vatNumberTextBox?.Text?.Trim();
            Company.AddressLine1 = addressLine1TextBox?.Text?.Trim();
            Company.AddressLine2 = addressLine2TextBox?.Text?.Trim();
            Company.City = cityTextBox?.Text?.Trim();
            Company.StateProvince = stateProvinceTextBox?.Text?.Trim();
            Company.Country = countryTextBox?.Text?.Trim();
            Company.PostalCode = postalCodeTextBox?.Text?.Trim();
            Company.Telephone = telephoneTextBox?.Text?.Trim();
            Company.Mobile = mobileTextBox?.Text?.Trim();
            Company.Email = emailTextBox?.Text?.Trim();
            Company.Website = websiteTextBox?.Text?.Trim();
            Company.Logo = logoTextBox?.Text?.Trim();
            Company.TaxRate = taxRateNumeric?.Value;
            Company.CurrencyCode = currencyCodeTextBox?.Text?.Trim();
            Company.IsActive = isActiveCheckBox?.Checked ?? true;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}