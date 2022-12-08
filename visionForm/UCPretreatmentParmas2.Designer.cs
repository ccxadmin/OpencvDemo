namespace visionForm
{
    partial class UCPretreatmentParmas2
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.NumMult = new UCLib.NumericUpDownEx();
            this.NumAdd = new UCLib.NumericUpDownEx();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NumMult)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumAdd)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "常数加:";
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(60, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "常数乘:";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.NumMult, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.NumAdd, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(140, 65);
            this.tableLayoutPanel1.TabIndex = 6;
            // 
            // NumMult
            // 
            this.NumMult.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.NumMult.DecimalPlaces = 2;
            this.NumMult.Font = new System.Drawing.Font("宋体", 9F);
            this.NumMult.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.NumMult.Location = new System.Drawing.Point(73, 3);
            this.NumMult.Name = "NumMult";
            this.NumMult.Size = new System.Drawing.Size(64, 25);
            this.NumMult.TabIndex = 6;
            this.NumMult.ValueChanged += new System.EventHandler(this.NumMult_ValueChanged);
            // 
            // NumAdd
            // 
            this.NumAdd.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.NumAdd.Font = new System.Drawing.Font("宋体", 9F);
            this.NumAdd.Location = new System.Drawing.Point(73, 36);
            this.NumAdd.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
            this.NumAdd.Name = "NumAdd";
            this.NumAdd.Size = new System.Drawing.Size(64, 25);
            this.NumAdd.TabIndex = 7;
            this.NumAdd.ValueChanged += new System.EventHandler(this.NumAdd_ValueChanged);
            // 
            // UCPretreatmentParmas2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UCPretreatmentParmas2";
            this.Size = new System.Drawing.Size(140, 65);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NumMult)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumAdd)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private UCLib.NumericUpDownEx NumMult;
        private UCLib.NumericUpDownEx NumAdd;
    }
}
