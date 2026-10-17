using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Bai3_ngay_17_10_2026
{
    public partial class Form1 : Form
    {
        private BindingList<RepairOrder> orders = new BindingList<RepairOrder>();
        private BindingSource bs = new BindingSource();

        public Form1()
        {
            InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            // Setup device types and statuses
            cbDeviceType.Items.AddRange(new object[] { "Phone", "Laptop", "Tablet", "TV", "Other" });
            cbStatus.Items.AddRange(Enum.GetNames(typeof(RepairStatus)));
            cbFilterStatus.Items.Add("All");
            cbFilterStatus.Items.AddRange(Enum.GetNames(typeof(RepairStatus)));
            cbFilterStatus.SelectedIndex = 0;

            // Setup binding
            bs.DataSource = orders;
            dgvOrders.DataSource = bs;

            // Make sure ReceivedDate column formats date
            dgvOrders.Columns[4].DefaultCellStyle.Format = "d";

            // Wire menu actions
            saveFileDialog1.Filter = "CSV files|*.csv|All files|*.*";

            // Example: prefill with a sample order (optional)
            orders.ListChanged += Orders_ListChanged;
            UpdateStatusInfo();
        }

        private void Orders_ListChanged(object sender, ListChangedEventArgs e)
        {
            UpdateStatusInfo();
        }

        private void UpdateStatusInfo()
        {
            int count = bs.Count;
            decimal pendingTotal = orders.Where(o => o.Status != RepairStatus.Completed && o.Status != RepairStatus.Cancelled)
                                         .Sum(o => o.EstimatedCost);
            toolStripStatusLabelInfo.Text = string.Format("{0} orders. Total pending cost: {1:N0}", count, pendingTotal);
        }

        private void RefreshBinding()
        {
            // Apply search and filter
            string q = txtSearch.Text.Trim().ToLower();
            string statusFilter = cbFilterStatus.SelectedItem as string;

            IEnumerable<RepairOrder> filtered = orders;
            if (!string.IsNullOrEmpty(q))
            {
                filtered = filtered.Where(o => (o.OrderId ?? "").ToLower().Contains(q)
                                             || (o.CustomerName ?? "").ToLower().Contains(q)
                                             || (o.Phone ?? "").ToLower().Contains(q));
            }
            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
            {
                if (Enum.TryParse<RepairStatus>(statusFilter, out var s))
                    filtered = filtered.Where(o => o.Status == s);
            }

            bs.DataSource = new BindingList<RepairOrder>(filtered.ToList());
            dgvOrders.DataSource = bs;
            UpdateStatusInfo();
        }

        private void btnChooseImage_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (var fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                    {
                        var img = Image.FromStream(fs);
                        pbDevice.Image = new Bitmap(img);
                    }
                    pbDevice.Tag = openFileDialog1.FileName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load image: " + ex.Message);
                }
            }
        }

        private void btnClearImage_Click(object sender, EventArgs e)
        {
            pbDevice.Image = null;
            pbDevice.Tag = null;
        }

        private bool ValidateForm(bool checkIdUnique)
        {
            errorProvider1.Clear();
            bool ok = true;
            if (string.IsNullOrWhiteSpace(txtOrderId.Text))
            {
                errorProvider1.SetError(txtOrderId, "Order ID required");
                ok = false;
            }
            else if (checkIdUnique && orders.Any(o => o.OrderId == txtOrderId.Text.Trim()))
            {
                errorProvider1.SetError(txtOrderId, "Order ID must be unique");
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                errorProvider1.SetError(txtCustomerName, "Customer name required");
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                errorProvider1.SetError(txtPhone, "Phone required");
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                errorProvider1.SetError(txtDescription, "Description required");
                ok = false;
            }
            if (!decimal.TryParse(txtEstimatedCost.Text, out var cost) || cost < 0)
            {
                errorProvider1.SetError(txtEstimatedCost, "Estimated cost must be a non-negative number");
                ok = false;
            }
            return ok;
        }

        private RepairOrder BuildOrderFromForm()
        {
            decimal.TryParse(txtEstimatedCost.Text, out var cost);
            var order = new RepairOrder
            {
                OrderId = txtOrderId.Text.Trim(),
                CustomerName = txtCustomerName.Text.Trim(),
                Phone = txtPhone.Text.Trim(),
                DeviceType = cbDeviceType.SelectedItem?.ToString() ?? string.Empty,
                Description = txtDescription.Text.Trim(),
                ReceivedDate = dtpReceived.Value.Date,
                EstimatedCost = cost,
                ImagePath = pbDevice.Tag as string
            };
            if (Enum.TryParse<RepairStatus>(cbStatus.SelectedItem?.ToString() ?? "New", out var s))
                order.Status = s;
            else
                order.Status = RepairStatus.New;

            return order;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateForm(true)) return;
            var order = BuildOrderFromForm();
            orders.Add(order);
            RefreshBinding();
            ClearForm();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            var selected = dgvOrders.CurrentRow.DataBoundItem as RepairOrder;
            if (selected == null) return;
            // When updating, allow same ID
            if (!ValidateForm(selected.OrderId != txtOrderId.Text.Trim())) return;
            // apply changes to original item in orders list
            var orig = orders.FirstOrDefault(o => o.OrderId == selected.OrderId);
            if (orig != null)
            {
                var updated = BuildOrderFromForm();
                orig.CustomerName = updated.CustomerName;
                orig.Phone = updated.Phone;
                orig.DeviceType = updated.DeviceType;
                orig.Description = updated.Description;
                orig.ReceivedDate = updated.ReceivedDate;
                orig.EstimatedCost = updated.EstimatedCost;
                orig.Status = updated.Status;
                orig.ImagePath = updated.ImagePath;
                // If OrderId changed ensure uniqueness then update
                if (orig.OrderId != updated.OrderId)
                {
                    if (orders.Any(o => o.OrderId == updated.OrderId))
                    {
                        MessageBox.Show("Order ID already exists.");
                        return;
                    }
                    orig.OrderId = updated.OrderId;
                }
                RefreshBinding();
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            var selected = dgvOrders.CurrentRow.DataBoundItem as RepairOrder;
            if (selected == null) return;
            var r = MessageBox.Show("Delete selected order?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes)
            {
                var orig = orders.FirstOrDefault(o => o.OrderId == selected.OrderId);
                if (orig != null) orders.Remove(orig);
                RefreshBinding();
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtOrderId.Text = "";
            txtCustomerName.Text = "";
            txtPhone.Text = "";
            cbDeviceType.SelectedIndex = -1;
            txtDescription.Text = "";
            dtpReceived.Value = DateTime.Today;
            txtEstimatedCost.Text = "0";
            cbStatus.SelectedItem = RepairStatus.New.ToString();
            pbDevice.Image = null;
            pbDevice.Tag = null;
            errorProvider1.Clear();
        }

        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null) return;
            var selected = dgvOrders.CurrentRow.DataBoundItem as RepairOrder;
            if (selected == null) return;
            txtOrderId.Text = selected.OrderId;
            txtCustomerName.Text = selected.CustomerName;
            txtPhone.Text = selected.Phone;
            cbDeviceType.SelectedItem = selected.DeviceType;
            txtDescription.Text = selected.Description;
            dtpReceived.Value = selected.ReceivedDate;
            txtEstimatedCost.Text = selected.EstimatedCost.ToString("F0");
            cbStatus.SelectedItem = selected.Status.ToString();
            if (!string.IsNullOrEmpty(selected.ImagePath) && File.Exists(selected.ImagePath))
            {
                try
                {
                    using (var fs = new FileStream(selected.ImagePath, FileMode.Open, FileAccess.Read))
                    {
                        var img = Image.FromStream(fs);
                        pbDevice.Image = new Bitmap(img);
                    }
                    pbDevice.Tag = selected.ImagePath;
                }
                catch
                {
                    pbDevice.Image = null;
                    pbDevice.Tag = null;
                }
            }
            else
            {
                pbDevice.Image = null;
                pbDevice.Tag = null;
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            RefreshBinding();
        }

        private void cbFilterStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshBinding();
        }

        private void exportCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
            try
            {
                var list = bs.List.Cast<RepairOrder>().ToList();
                using (var sw = new StreamWriter(saveFileDialog1.FileName, false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("OrderId,Customer,Phone,DeviceType,ReceivedDate,EstimatedCost,Status,ImagePath,Description");
                    foreach (var o in list)
                    {
                        var line = string.Format("\"{0}\",\"{1}\",\"{2}\",\"{3}\",{4},\"{5}\",\"{6}\",\"{7}\",\"{8}\"",
                            o.OrderId, o.CustomerName, o.Phone, o.DeviceType, o.ReceivedDate.ToString("yyyy-MM-dd"), o.EstimatedCost.ToString("F0"), o.Status, o.ImagePath, o.Description.Replace("\"", "''"));
                        sw.WriteLine(line);
                    }
                }
                MessageBox.Show("Exported successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
