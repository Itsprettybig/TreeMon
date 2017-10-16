﻿using PluginInterface;
using System;
using System.IO;
using System.Windows.Forms;
using TreeMon.Utilites.Helpers;

namespace TreeMon.Client
{
    public partial class frmMain : Form
    {
        bool _firstRun = false;

        #region  CONTROLS 

        private System.Windows.Forms.MainMenu mainMenu;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.StatusBar statusBar;
        private System.Windows.Forms.StatusBarPanel statusBarPanel;
        private System.Windows.Forms.TreeView tvwPlugins;
        private System.Windows.Forms.Panel pnlPlugin;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblPluginName;
        private System.Windows.Forms.Label lblPluginVersion;
        private System.Windows.Forms.Label lblPluginAuthor;
        private System.Windows.Forms.Label lblPluginDesc;

        #endregion

        #region Members
        private string _appPath = string.Empty;
        private string _currentPlugin = string.Empty;
        private string _procLogFile = string.Empty;
        private string _initFile = string.Empty;
        private string[] _programArgs;
        private SplitContainer splitContainer1;
        private Label lblStatus;
        #endregion

        public frmMain MainForm { get { return this; } }

        public frmMain(string[] args)
        {
            InitializeComponent();

        }


        private void frmMain_Load(object sender, System.EventArgs e)
        {
            string pathToInstallCommands = EnvironmentEx.AppDataFolder + "Install\\install.json";

            if (!File.Exists(pathToInstallCommands))
            {
                _firstRun = true;
                Install();
                return;
            }

            Initialize();
        }

        private void Install()
        {
            frmInstall install = new frmInstall();
            if (install.ShowDialog() != DialogResult.OK)
            {
                this.Close();
                return;
            }

        }

        private void Initialize()
        {
    

            frmLogin login = new frmLogin();
            if (login.ShowDialog() != DialogResult.OK)
            {
                this.Close();
                return;
            }



            string result = "";

            if (string.IsNullOrEmpty(_currentPlugin))
                result = LoadPlugins();//loads plugins in the treeview.
            else
            {    //if a specific plugin is passed in through command line, then only load that one.
                result = Global.Plugins.LoadPlugin(Application.StartupPath + @"\Plugins\" + _currentPlugin + ".dll", _programArgs);
                lblStatus.Text = result;

                // if single plugin is loaded, pass args to plugin..
                //
                AvailablePlugin selectedPlugin = Global.Plugins.AvailablePlugins.Find(_currentPlugin);

                if (selectedPlugin != null)
                {
                    selectedPlugin.Instance.Initialize(_programArgs);//initialize the plugin.

                    TreeNode newNode = new TreeNode(selectedPlugin.Instance.ShortName); //show the plugin in the host list.
                    this.tvwPlugins.Nodes.Add(newNode);
                }
            }
            lblStatus.Text = result;
        }

        /// <summary>
        /// loads treeview with plugins in the plugin directory..
        /// </summary>
        /// <returns></returns>
        private string LoadPlugins()
        {
            string pluginDirectory = Application.StartupPath + @"\Plugins";

            if (!Directory.Exists(pluginDirectory))
            {
                return "Plugin directory not found in:" + pluginDirectory;
            }

                //Call the find plugins routine, to search in our Plugins Folder
                string  result = Global.Plugins.FindPlugins(pluginDirectory);

            //TODO: load application settings with plugin sort order. Add the plugins based on sort order.

            //Add each plugin to the treeview
            foreach (AvailablePlugin pluginOn in Global.Plugins.AvailablePlugins)
            {
                TreeNode newNode = new TreeNode(pluginOn.Instance.ShortName);
                this.tvwPlugins.Nodes.Add(newNode);
                newNode = null;
            }
            return result;
        }

        private void tvwPlugins_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            //Make sure there's a selected Plugin
            if (this.tvwPlugins.SelectedNode == null)
                return;

            //Get the selected Plugin
            AvailablePlugin selectedPlugin = Global.Plugins.AvailablePlugins.Find(tvwPlugins.SelectedNode.Text.ToString());

            if (selectedPlugin == null)
                return;

            //If the plugin is found, do some work...
             //This part adds the plugin's info to the 'Plugin Information:' Frame
             this.lblPluginName.Text = selectedPlugin.Instance.Name;
             this.lblPluginVersion.Text = "(" + selectedPlugin.Instance.Version + ")";
             this.lblPluginAuthor.Text = "By: " + selectedPlugin.Instance.Author;
             this.lblPluginDesc.Text = selectedPlugin.Instance.Description;

             //Clear the current panel of any other plugin controls... 
             //Note: this only affects visuals.. doesn't close the instance of the plugin
             this.pnlPlugin.Controls.Clear();

             //Set the dockstyle of the plugin to fill, to fill up the space provided
             selectedPlugin.Instance.MainInterface.Dock = DockStyle.Fill;

             //Finally, add the usercontrol to the panel... Tadah!
             this.pnlPlugin.Controls.Add(selectedPlugin.Instance.MainInterface);
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            splitContainer1.Dock = DockStyle.Fill; //MUST update the splitter for the resize message to pump down to the plugin.
            //If you want to mannually resize.
            //Make sure there's a selected Plugin
            //if (this.tvwPlugins.SelectedNode != null)
            //{
            //    //Get the selected Plugin
            //    Types.AvailablePlugin selectedPlugin = Global.Plugins.AvailablePlugins.Find(tvwPlugins.SelectedNode.Text.ToString());

            //    if (selectedPlugin != null)
            //    {
            //        //Set the dockstyle of the plugin to fill, to fill up the space provided
            //        //selectedPlugin.Instance.MainInterface.Dock = DockStyle.Fill;
            //        selectedPlugin.Instance.ResizeControl();
            //    }

            //}
        }

        private void menuItem2_Click(object sender, System.EventArgs e)
        {
            this.Close(); //User clicked File > Exit
        }

        private void frmMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Lets call the close for all the plugins before we truly exit!
            Global.Plugins.ClosePlugins();
        }

        private void mnuNewProject_Click(object sender, EventArgs e)
        {
            //TODO: GET THE name of the current selected plugin.
            //Then call function to display the project gui,
            //  the plugin must have ability to clear the display etc.

            // this.pnlPlugin.Controls.Add(
            AvailablePlugin selectedPlugin = Global.Plugins.AvailablePlugins.Find(tvwPlugins.SelectedNode.Text.ToString());

            if (selectedPlugin != null)
            {
                //selectedPlugin.Instance.MainInterface
            }
        }



     

      
    }
}