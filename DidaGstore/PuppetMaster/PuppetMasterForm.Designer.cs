namespace PuppetMaster {
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
            // PuppetMasterForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 338);
            this.Controls.Add(this.btnOpenScript);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "PuppetMasterForm";
            this.Text = "Puppet Master";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnOpenScript;
        private System.Windows.Forms.OpenFileDialog openScriptDialog;
    }
}

