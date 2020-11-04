﻿namespace PuppetMaster {
    partial class PuppetMasterForm {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.openScriptDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnOpenScript = new System.Windows.Forms.Button();
            this.btnKill = new System.Windows.Forms.Button();
            this.txtKill = new System.Windows.Forms.TextBox();
            this.btnStatus = new System.Windows.Forms.Button();
            this.btnFreeze = new System.Windows.Forms.Button();
            this.btnUnfreeze = new System.Windows.Forms.Button();
            this.txtFreeze = new System.Windows.Forms.TextBox();
            this.txtUnfreeze = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // openScriptDialog
            // 
            this.openScriptDialog.FileName = "openScriptDialog";
            // 
            // btnOpenScript
            // 
            this.btnOpenScript.Location = new System.Drawing.Point(31, 26);
            this.btnOpenScript.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnOpenScript.Name = "btnOpenScript";
            this.btnOpenScript.Size = new System.Drawing.Size(113, 28);
            this.btnOpenScript.TabIndex = 0;
            this.btnOpenScript.Text = "Open Script";
            this.btnOpenScript.UseVisualStyleBackColor = true;
            this.btnOpenScript.Click += new System.EventHandler(this.btnOpenScript_Click);
            // 
            // btnKill
            // 
            this.btnKill.Location = new System.Drawing.Point(31, 87);
            this.btnKill.Name = "btnKill";
            this.btnKill.Size = new System.Drawing.Size(335, 134);
            this.btnKill.TabIndex = 1;
            this.btnKill.Text = "Kill";
            this.btnKill.UseVisualStyleBackColor = true;
            this.btnKill.Click += new System.EventHandler(this.btnKill_Click);
            // 
            // txtKill
            // 
            this.txtKill.Location = new System.Drawing.Point(391, 87);
            this.txtKill.Name = "txtKill";
            this.txtKill.Size = new System.Drawing.Size(100, 23);
            this.txtKill.TabIndex = 2;
            // 
            // btnStatus
            // 
            this.btnStatus.Location = new System.Drawing.Point(31, 303);
            this.btnStatus.Name = "btnStatus";
            this.btnStatus.Size = new System.Drawing.Size(75, 23);
            this.btnStatus.TabIndex = 3;
            this.btnStatus.Text = "Status";
            this.btnStatus.UseVisualStyleBackColor = true;
            this.btnStatus.Click += new System.EventHandler(this.btnStatus_Click);
            // 
            // btnFreeze
            // 
            this.btnFreeze.Location = new System.Drawing.Point(31, 242);
            this.btnFreeze.Name = "btnFreeze";
            this.btnFreeze.Size = new System.Drawing.Size(75, 23);
            this.btnFreeze.TabIndex = 4;
            this.btnFreeze.Text = "Freeze";
            this.btnFreeze.UseVisualStyleBackColor = true;
            this.btnFreeze.Click += new System.EventHandler(this.btnFreeze_Click);
            // 
            // btnUnfreeze
            // 
            this.btnUnfreeze.Location = new System.Drawing.Point(31, 272);
            this.btnUnfreeze.Name = "btnUnfreeze";
            this.btnUnfreeze.Size = new System.Drawing.Size(75, 23);
            this.btnUnfreeze.TabIndex = 5;
            this.btnUnfreeze.Text = "Unfreeze";
            this.btnUnfreeze.UseVisualStyleBackColor = true;
            this.btnUnfreeze.Click += new System.EventHandler(this.btnUnfreeze_Click);
            // 
            // txtFreeze
            // 
            this.txtFreeze.Location = new System.Drawing.Point(132, 242);
            this.txtFreeze.Name = "txtFreeze";
            this.txtFreeze.Size = new System.Drawing.Size(100, 23);
            this.txtFreeze.TabIndex = 6;
            // 
            // txtUnfreeze
            // 
            this.txtUnfreeze.Location = new System.Drawing.Point(132, 273);
            this.txtUnfreeze.Name = "txtUnfreeze";
            this.txtUnfreeze.Size = new System.Drawing.Size(100, 23);
            this.txtUnfreeze.TabIndex = 6;
            // 
            // PuppetMasterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 338);
            this.Controls.Add(this.txtUnfreeze);
            this.Controls.Add(this.txtFreeze);
            this.Controls.Add(this.btnUnfreeze);
            this.Controls.Add(this.btnFreeze);
            this.Controls.Add(this.btnStatus);
            this.Controls.Add(this.txtKill);
            this.Controls.Add(this.btnKill);
            this.Controls.Add(this.btnOpenScript);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "PuppetMasterForm";
            this.Text = "Puppet Master";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnOpenScript;
        private System.Windows.Forms.OpenFileDialog openScriptDialog;
        private System.Windows.Forms.Button btnKill;
        private System.Windows.Forms.TextBox txtKill;
        private System.Windows.Forms.Button btnStatus;
        private System.Windows.Forms.Button btnFreeze;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnUnfreeze;
        private System.Windows.Forms.TextBox txtFreeze;
        private System.Windows.Forms.TextBox txtUnfreeze;
    }
}

