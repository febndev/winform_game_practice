namespace SpaceShooter
{
    partial class Loginform
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Loginform));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.idLabel = new System.Windows.Forms.Label();
            this.pwLabel = new System.Windows.Forms.Label();
            this.idTb = new System.Windows.Forms.TextBox();
            this.pwTb = new System.Windows.Forms.TextBox();
            this.signupBtn = new System.Windows.Forms.Button();
            this.loginBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Impact", 48F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(12, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(202, 80);
            this.label1.TabIndex = 0;
            this.label1.Text = "Space";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Impact", 48F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.Location = new System.Drawing.Point(106, 111);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(250, 80);
            this.label2.TabIndex = 1;
            this.label2.Text = "Shooter";
            // 
            // idLabel
            // 
            this.idLabel.AutoSize = true;
            this.idLabel.Font = new System.Drawing.Font("Impact", 20.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.idLabel.ForeColor = System.Drawing.Color.Gold;
            this.idLabel.Location = new System.Drawing.Point(24, 210);
            this.idLabel.Name = "idLabel";
            this.idLabel.Size = new System.Drawing.Size(40, 34);
            this.idLabel.TabIndex = 2;
            this.idLabel.Text = "ID";
            // 
            // pwLabel
            // 
            this.pwLabel.AutoSize = true;
            this.pwLabel.Font = new System.Drawing.Font("Impact", 20.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pwLabel.ForeColor = System.Drawing.Color.Gold;
            this.pwLabel.Location = new System.Drawing.Point(24, 302);
            this.pwLabel.Name = "pwLabel";
            this.pwLabel.Size = new System.Drawing.Size(53, 34);
            this.pwLabel.TabIndex = 3;
            this.pwLabel.Text = "PW";
            // 
            // idTb
            // 
            this.idTb.Location = new System.Drawing.Point(26, 256);
            this.idTb.Name = "idTb";
            this.idTb.Size = new System.Drawing.Size(330, 21);
            this.idTb.TabIndex = 4;
            // 
            // pwTb
            // 
            this.pwTb.Location = new System.Drawing.Point(26, 348);
            this.pwTb.Name = "pwTb";
            this.pwTb.Size = new System.Drawing.Size(330, 21);
            this.pwTb.TabIndex = 5;
            this.pwTb.UseSystemPasswordChar = true;
            // 
            // signupBtn
            // 
            this.signupBtn.Font = new System.Drawing.Font("돋움", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.signupBtn.Location = new System.Drawing.Point(26, 407);
            this.signupBtn.Name = "signupBtn";
            this.signupBtn.Size = new System.Drawing.Size(113, 23);
            this.signupBtn.TabIndex = 6;
            this.signupBtn.Text = "회원가입";
            this.signupBtn.UseVisualStyleBackColor = true;
            // 
            // loginBtn
            // 
            this.loginBtn.Font = new System.Drawing.Font("돋움", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.loginBtn.Location = new System.Drawing.Point(163, 407);
            this.loginBtn.Name = "loginBtn";
            this.loginBtn.Size = new System.Drawing.Size(193, 23);
            this.loginBtn.TabIndex = 7;
            this.loginBtn.Text = "로그인";
            this.loginBtn.UseVisualStyleBackColor = true;
            this.loginBtn.Click += new System.EventHandler(this.loginBtn_Click);
            // 
            // Loginform
            // 
            this.AcceptButton = this.loginBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Navy;
            this.ClientSize = new System.Drawing.Size(384, 461);
            this.Controls.Add(this.loginBtn);
            this.Controls.Add(this.signupBtn);
            this.Controls.Add(this.pwTb);
            this.Controls.Add(this.idTb);
            this.Controls.Add(this.pwLabel);
            this.Controls.Add(this.idLabel);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Loginform";
            this.Text = "Space Shooter - Log in ";
            this.Load += new System.EventHandler(this.Loginform_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label idLabel;
        private System.Windows.Forms.Label pwLabel;
        private System.Windows.Forms.TextBox idTb;
        private System.Windows.Forms.TextBox pwTb;
        private System.Windows.Forms.Button signupBtn;
        private System.Windows.Forms.Button loginBtn;
    }
}