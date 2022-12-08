namespace visionForm
{
    partial class UCPretreatmentParmas
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
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.cobxMaskWidth = new UCLib.ComboBoxEx();
            this.cobxMarkHeight = new UCLib.ComboBoxEx();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "掩膜宽度:";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 15);
            this.label2.TabIndex = 1;
            this.label2.Text = "掩膜高度:";
            // 
            // errorProvider1
            // 
            this.errorProvider1.BlinkRate = 1000;
            this.errorProvider1.BlinkStyle = System.Windows.Forms.ErrorBlinkStyle.AlwaysBlink;
            this.errorProvider1.ContainerControl = this;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.cobxMaskWidth, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.cobxMarkHeight, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(170, 70);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // cobxMaskWidth
            // 
            this.cobxMaskWidth.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.cobxMaskWidth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cobxMaskWidth.FormattingEnabled = true;
            this.cobxMaskWidth.Items.AddRange(new object[] {
            "3",
            "5",
            "7",
            "9",
            "11",
            "13",
            "15"});
            this.cobxMaskWidth.Location = new System.Drawing.Point(88, 6);
            this.cobxMaskWidth.Name = "cobxMaskWidth";
            this.cobxMaskWidth.Size = new System.Drawing.Size(79, 23);
            this.cobxMaskWidth.TabIndex = 4;
            this.cobxMaskWidth.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cobxMaskWidth_KeyPress);
            this.cobxMaskWidth.KeyUp += new System.Windows.Forms.KeyEventHandler(this.cobxMaskWidth_KeyUp);
            // 
            // cobxMarkHeight
            // 
            this.cobxMarkHeight.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.cobxMarkHeight.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cobxMarkHeight.FormattingEnabled = true;
            this.cobxMarkHeight.Items.AddRange(new object[] {
            "3",
            "5",
            "7",
            "9",
            "11",
            "13",
            "15"});
            this.cobxMarkHeight.Location = new System.Drawing.Point(88, 41);
            this.cobxMarkHeight.Name = "cobxMarkHeight";
            this.cobxMarkHeight.Size = new System.Drawing.Size(79, 23);
            this.cobxMarkHeight.TabIndex = 5;
            this.cobxMarkHeight.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cobxMarkHeight_KeyPress);
            this.cobxMarkHeight.KeyUp += new System.Windows.Forms.KeyEventHandler(this.cobxMarkHeight_KeyUp);
            // 
            // UCPretreatmentParmas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UCPretreatmentParmas";
            this.Size = new System.Drawing.Size(170, 70);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private UCLib.ComboBoxEx cobxMaskWidth;
        private UCLib.ComboBoxEx cobxMarkHeight;
    }
}
