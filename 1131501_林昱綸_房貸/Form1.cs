using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _1131501_林昱綸_房貸
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // 預設值
            txtHousePrice.Text = "10000000";
            txtDownPayment.Text = "20";
            txtInterest.Text = "2";
            txtYears.Text = "30";
            txtGrace.Text = "0";

            this.AcceptButton = btnCalculate; // 按 Enter 執行計算
            // 綁定按鈕事件
            btnCalculate.Click += btnCalculate_Click;
            btnExport.Click += btnExport_Click;
        }
        private void Calculate()
        {
            lblError.Text = "";

            // 1. 讀取使用者輸入
            if (!double.TryParse(txtHousePrice.Text, out double housePrice) ||
                !double.TryParse(txtDownPayment.Text, out double down) ||
                !double.TryParse(txtInterest.Text, out double rate) ||
                !int.TryParse(txtYears.Text, out int years) ||
                !int.TryParse(txtGrace.Text, out int graceYears))
            {
                lblError.Text = "輸入錯誤";
                MessageBox.Show("請確認所有欄位都輸入正確數字！", "輸入錯誤",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
           

            // 2. 計算貸款本金、月利率、期數
            double loan = housePrice * (1 - down / 100); // 貸款本金
            double monthlyRate = rate / 100 / 12;        // 月利率
            int months = years * 12;                     // 總期數
            int graceMonths = graceYears * 12;          // 寬限期月數

            // 3. 月付金額（等額本息公式）
            double monthlyPayment = loan * (monthlyRate * Math.Pow(1 + monthlyRate, months)) /
                                    (Math.Pow(1 + monthlyRate, months) - 1);

            // 4. 初始化累積利息
            double totalInterest = 0;
            double balance = loan;

            // 5. 清空 DataGridView
            dataGridView1.Rows.Clear();

            int repayMonths = months - graceMonths;

            double monthlyPaymentAfterGrace;

            if (monthlyRate == 0)
            {
                monthlyPaymentAfterGrace = loan / repayMonths;
            }
            else
            {
                monthlyPaymentAfterGrace =
                    loan * (monthlyRate * Math.Pow(1 + monthlyRate, repayMonths)) /
                    (Math.Pow(1 + monthlyRate, repayMonths) - 1);
            }



            // 6. 每月計算本金、利息、剩餘本金
            for (int i = 1; i <= months; i++)
            {
                double interest = balance * monthlyRate; // 當月利息
                double principal;
                double payment;

                if (i <= graceMonths)
                {
                    payment = interest;   // 只繳利息
                    principal = 0;
                }
                else
                {
                    payment = monthlyPaymentAfterGrace; // 正式還款
                    principal = payment - interest;
                }

                balance -= principal;      // 更新剩餘本金
                balance = Math.Max(balance, 0); // 避免顯示微小負數
                totalInterest += interest; // 累積利息

                // 7. 加入 DataGridView
                dataGridView1.Rows.Add(i, principal.ToString("N2"), interest.ToString("N2"), balance.ToString("N2"));
                
            }

            // 8. 顯示結果
            lblLoanAmount.Text = loan.ToString("N2");
            lblMonthly.Text = monthlyPaymentAfterGrace.ToString("N2");
            lblTotalInterest.Text = totalInterest.ToString("N2");
            lblTotalPayment.Text = (loan + totalInterest).ToString("N2");
            if (dataGridView1.Rows.Count > 0)
            {
                var firstRow = dataGridView1.Rows[0];

                string principal = firstRow.Cells[1].Value?.ToString() ?? "0";
                string interest = firstRow.Cells[2].Value?.ToString() ?? "0";

                lblFirstPrincipal.Text = principal;
                lblFirstInterest.Text = interest;
            }
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            Calculate();
        }

        bool isDarkMode = false;
        
        private void btnTheme_Click(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode; // 切換狀態

            if (isDarkMode)
            {
                // 🌙 深色模式
                this.BackColor = Color.FromArgb(30, 42, 56);

                foreach (Control c in this.Controls)
                {
                    c.ForeColor = Color.White;
                    c.BackColor = Color.FromArgb(45, 45, 45);
                }

                // DataGridView
                dataGridView1.BackgroundColor = Color.FromArgb(45, 45, 45);
                dataGridView1.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
                dataGridView1.DefaultCellStyle.ForeColor = Color.White;
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 30);
                dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                dataGridView1.EnableHeadersVisualStyles = false;
            }
            else
            {
                // ☀️ 淺色模式（還原）
                this.BackColor = SystemColors.Control;

                foreach (Control c in this.Controls)
                {
                    c.ForeColor = Color.Black;
                    c.BackColor = SystemColors.Control;
                }

                // DataGridView 還原
                dataGridView1.BackgroundColor = Color.White;
                dataGridView1.DefaultCellStyle.BackColor = Color.White;
                dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
                dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
                dataGridView1.EnableHeadersVisualStyles = true;
            }
            btnTheme.Text = isDarkMode ? "☀️ 淺色模式" : "🌙 深色模式";
        }
        

        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV檔案|*.csv";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.WriteLine("期數,本金,利息,剩餘本金");
                    // 逐行讀取 DataGridView
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (!row.IsNewRow)
                        {
                            sw.WriteLine(string.Join(",",
                                row.Cells[0].Value,
                                row.Cells[1].Value,
                                row.Cells[2].Value,
                                row.Cells[3].Value));
                        }
                    }
                }
                MessageBox.Show("匯出成功！");
            }
        }

        private void trackBarInterest_Scroll(object sender, EventArgs e)
        {
            txtInterest.Text = trackBarInterest.Value.ToString();
            lblRate.Text = trackBarInterest.Value + "%";
        }

        private void trackBarYears_Scroll(object sender, EventArgs e)
        {
            txtYears.Text = trackBarYears.Value.ToString();
            lblYears.Text = trackBarYears.Value + " 年";
        }
        private void txt_TextChanged(object sender, EventArgs e)
 {
    Calculate();
 }
    }
}
