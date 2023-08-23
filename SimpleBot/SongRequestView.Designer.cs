namespace SimpleBot
{
  partial class SongRequestView
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      btnShowYoutubeForm = new Button();
      SuspendLayout();
      // 
      // btnShowYoutubeForm
      // 
      btnShowYoutubeForm.Location = new Point(3, 3);
      btnShowYoutubeForm.Name = "btnShowYoutubeForm";
      btnShowYoutubeForm.Size = new Size(115, 23);
      btnShowYoutubeForm.TabIndex = 8;
      btnShowYoutubeForm.Text = "YouTube Player";
      btnShowYoutubeForm.UseVisualStyleBackColor = true;
      btnShowYoutubeForm.Click += btnShowYoutubeForm_Click;
      // 
      // SongRequestView
      // 
      AutoScaleDimensions = new SizeF(7F, 15F);
      AutoScaleMode = AutoScaleMode.Font;
      BorderStyle = BorderStyle.FixedSingle;
      Controls.Add(btnShowYoutubeForm);
      Name = "SongRequestView";
      Size = new Size(331, 252);
      ResumeLayout(false);
    }

    #endregion

    private Button btnShowYoutubeForm;
  }
}
