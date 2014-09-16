﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace PerniciousGames.OpenFileInSolution
{
    public class ProjectItemWrapper
    {
        public string Filename { get; set; }
        public ProjectItem ProjItem;

        private ProjectItemWrapper()
        {

        }

        public ProjectItemWrapper(ProjectItem inItem)
        {
            ProjItem = inItem;
            Filename = inItem.FileNames[1];
        }
    }

    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidOpenFileInSolutionPkgString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class OpenFileInSolutionPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public OpenFileInSolutionPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        public static DTE2 GetActiveIDE()
        {
            // Get an instance of currently running Visual Studio IDE.
            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            return dte2;
        }

        public static IList<Project> Projects()
        {
            Projects projects = GetActiveIDE().Solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        private static IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }

        static readonly Guid ProjectFileGuid = new Guid("6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C");
        static readonly Guid ProjectFolderGuid = new Guid("6BB5F8F0-4483-11D3-8BCF-00C04F8EC28C");

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidOpenFileInSolutionCmdSet, 0x100);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
            // todo: add option to jump between h and cpp?
        }

        private IEnumerable<ProjectItemWrapper> EnumerateProjectItems(ProjectItems items)
        {
            for (int i = 1; i <= items.Count; i++)
            {
                var itm = items.Item(i);
                if (Guid.Parse(itm.Kind).Equals(ProjectFolderGuid))
                {
                    foreach (var res in EnumerateProjectItems(itm.ProjectItems))
                    {
                        yield return res;
                    }
                }
                else
                {
                    //Debug.WriteLine(itm.Kind + " - " + itm.FileCount + " - " + itm.FileNames[0]);
                    for (short j = 0; itm != null && j < itm.FileCount; j++)
                    {
                        yield return new ProjectItemWrapper(itm);
                    }
                }
            }
        }
        #endregion

        private void MenuItemCallback(object sender, EventArgs e)
        {
            var projItems = new List<ProjectItemWrapper>();
            foreach (var proj in Projects())
            {
                projItems.AddRange(EnumerateProjectItems(proj.ProjectItems));
            }

            new ListFiles(projItems).ShowDialog();
        }
    }
}
