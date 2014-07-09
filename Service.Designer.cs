namespace BalanceChecker
{
	partial class Service
	{
		/// <summary> 
		/// Требуется переменная конструктора.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Освободить все используемые ресурсы.
		/// </summary>
		/// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Код, автоматически созданный конструктором компонентов

		/// <summary> 
		/// Обязательный метод для поддержки конструктора - не изменяйте 
		/// содержимое данного метода при помощи редактора кода.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Service));
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.notifyIconMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.itemUpdateConfig = new System.Windows.Forms.ToolStripMenuItem();
			this.notifyIconMenu.SuspendLayout();
			// 
			// timer
			// 
			this.timer.Tick += new System.EventHandler(this.timer_Tick);
			// 
			// notifyIcon
			// 
			this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
			this.notifyIcon.Text = "Balance Checker";
			this.notifyIcon.Visible = true;
			// 
			// notifyIconMenu
			// 
			this.notifyIconMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.itemUpdateConfig});
			this.notifyIconMenu.Name = "notifyIconMenu";
			this.notifyIconMenu.Size = new System.Drawing.Size(216, 26);
			// 
			// itemUpdateConfig
			// 
			this.itemUpdateConfig.Name = "itemUpdateConfig";
			this.itemUpdateConfig.Size = new System.Drawing.Size(215, 22);
			this.itemUpdateConfig.Text = "Обновить конфигурацию";
			// 
			// Service
			// 
			this.CanPauseAndContinue = true;
			this.ServiceName = "BalanceChecker";
			this.notifyIconMenu.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ContextMenuStrip notifyIconMenu;
		private System.Windows.Forms.ToolStripMenuItem itemUpdateConfig;
	}
}
