﻿using SamirBoulema.TGit.Helpers;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace SamirBoulema.TGit.Commands
{
    public class GitFlowMenuCommands
    {
        private readonly OptionPageGrid _options;
        private readonly OleMenuCommandService _mcs;

        public GitFlowMenuCommands(OleMenuCommandService mcs, OptionPageGrid options)
        {
            _mcs = mcs;
            _options = options;
        }

        public void AddCommands()
        {
            //GitFlow Commands
            //Start/Finish Feature
            var startFeature = CommandHelper.CreateCommand(StartFeatureCommand, PkgCmdIDList.StartFeature);
            startFeature.BeforeQueryStatus += CommandHelper.GitFlow_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, startFeature);

            var finishFeature = CommandHelper.CreateCommand(FinishFeatureCommand, PkgCmdIDList.FinishFeature);
            finishFeature.BeforeQueryStatus += CommandHelper.Feature_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, finishFeature);

            //Start/Finish Release
            var startRelease = CommandHelper.CreateCommand(StartReleaseCommand, PkgCmdIDList.StartRelease);
            startRelease.BeforeQueryStatus += CommandHelper.GitFlow_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, startRelease);

            var finishRelease = CommandHelper.CreateCommand(FinishReleaseCommand, PkgCmdIDList.FinishRelease);
            finishRelease.BeforeQueryStatus += CommandHelper.Release_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, finishRelease);

            //Start/Finish Hotfix
            var startHotfix = CommandHelper.CreateCommand(StartHotfixCommand, PkgCmdIDList.StartHotfix);
            startHotfix.BeforeQueryStatus += CommandHelper.GitFlow_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, startHotfix);

            var finishHotfix = CommandHelper.CreateCommand(FinishHotfixCommand, PkgCmdIDList.FinishHotfix);
            finishHotfix.BeforeQueryStatus += CommandHelper.Hotfix_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, finishHotfix);

            //Start/Finish Feature-Branch
            var startFeatureBranch = CommandHelper.CreateCommand(StartFeatureBranchCommand, PkgCmdIDList.StartFeatureBranch);
            startFeatureBranch.BeforeQueryStatus += CommandHelper.GitFlow_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, startFeatureBranch);

            var finishFeatureBranch = CommandHelper.CreateCommand(FinishFeatureBranchCommand, PkgCmdIDList.FinishFeatureBranch);
            finishFeatureBranch.BeforeQueryStatus += CommandHelper.FeatureBranch_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, finishFeatureBranch);

            //Init
            var init = CommandHelper.CreateCommand(InitCommand, PkgCmdIDList.Init);
            init.BeforeQueryStatus += CommandHelper.GitHubFlow_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, init);

            //GitHubFlow Commands
            //Start/Finish Feature
            CommandHelper.AddCommand(_mcs, StartFeatureGitHubCommand, PkgCmdIDList.StartFeatureGitHub);
            var finishFeatureGitHub = CommandHelper.CreateCommand(FinishFeatureGitHubCommand, PkgCmdIDList.FinishFeatureGitHub);
            finishFeatureGitHub.BeforeQueryStatus += CommandHelper.Feature_BeforeQueryStatus;
            CommandHelper.AddCommand(_mcs, finishFeatureGitHub);
        }

        private void InitCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;

            var flowDialog = new FlowDialog();
            if (flowDialog.ShowDialog() != DialogResult.OK) return;

            var versionTag = string.IsNullOrEmpty(flowDialog.GitConfig.TagPrefix) ? "\"\"" : flowDialog.GitConfig.TagPrefix;

            /* 1. Add GitFlow config options
                 * 2. Checkout develop branch (create if it doesn't exist, reset if it does)
                 * 3. Push develop branch
                 */
            var process = ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                GitHelper.GetSshSetup() +
                FormatCliCommand($"config --add gitflow.branch.master {flowDialog.GitConfig.MasterBranch}") +
                FormatCliCommand($"config --add gitflow.branch.develop {flowDialog.GitConfig.DevelopBranch}") +
                FormatCliCommand($"config --add gitflow.prefix.feature {flowDialog.GitConfig.FeaturePrefix}") +
                FormatCliCommand($"config --add gitflow.prefix.release {flowDialog.GitConfig.ReleasePrefix}") +
                FormatCliCommand($"config --add gitflow.prefix.hotfix {flowDialog.GitConfig.HotfixPrefix}") +
                FormatCliCommand($"config --add gitflow.prefix.versiontag {versionTag}") +
                (GitHelper.RemoteBranchExists(flowDialog.GitConfig.DevelopBranch) ?
                    "echo." :
                    FormatCliCommand($"checkout -b {flowDialog.GitConfig.DevelopBranch}", false)),
                "Initializing GitFlow"
                );
            process.WaitForExit();

            EnvHelper.GetGitConfig();
            EnvHelper.GetBranchName();
        }

        private void StartFeatureCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var featureName = Interaction.InputBox("Feature Name:", "Start New Feature");
            if (string.IsNullOrEmpty(featureName)) return;

            var flowOptions = GitHelper.GetGitConfig();

            /* 1. Switch to the develop branch
             * 2. Pull latest changes on develop
             * 3. Create and switch to a new branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {flowOptions.DevelopBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"checkout -b {flowOptions.FeaturePrefix}{featureName} {flowOptions.DevelopBranch}", false),
                $"Starting feature {featureName}"
            );
        }

        private void StartFeatureBranchCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EnvHelper.SolutionDir)) return;
            var featureBranchName = Interaction.InputBox("Feature-branch name:", "Start new feature-branch");
            if (string.IsNullOrWhiteSpace(featureBranchName)) return;

            var flowOptions = GitHelper.GetGitConfig();

            /* 1. Switch to the feature main branch
             * 2. Pull latest changes on feature main
             * 3. Create and switch to a new branch
             */

            string featureMainBranch = GitHelper.GetCurrentBranchName(false);
            if (!featureMainBranch.EndsWith(GitHelper.mainFeatureBranchSuffix))
            {
                return;
            }

            string featureMainBranchPrefix = featureMainBranch.Substring(0, featureMainBranch.Length - GitHelper.mainFeatureBranchSuffix.Length);

            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {featureMainBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"checkout -b {featureMainBranchPrefix}{featureBranchName} {featureMainBranch}", false),
                $"Starting feature-branch {featureBranchName}"
            );
        }

        private void StartFeatureGitHubCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var featureName = Interaction.InputBox("Feature Name:", "Start New Feature");
            if (string.IsNullOrEmpty(featureName)) return;

            /* 1. Switch to the master branch
             * 2. Pull latest changes on master
             * 3. Create and switch to a new branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand("checkout master") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"checkout -b {featureName} master", false),
                $"Starting feature {featureName}"
            );
        }

        private void FinishFeatureCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var featureBranch = GitHelper.GetCurrentBranchName(false);
            var featureName = GitHelper.GetCurrentBranchName(true);
            EnvHelper.GetGitConfig();

            /* 1. Switch to the develop branch
             * 2. Pull latest changes on develop
             * 3. Merge the feature branch to develop
             * 4. Push all changes to develop
             * 5. Delete the local feature branch
             * 6. Delete the remote feature branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {EnvHelper.GitConfig.DevelopBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"merge --no-ff {featureBranch}", false),
                $"Finishing feature {featureName}",
                featureBranch, null, _options, FormatCliCommand($"push origin {EnvHelper.GitConfig.DevelopBranch}")
            );
        }

        private void FinishFeatureBranchCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var featureBranch = GitHelper.GetCurrentBranchName(false);
            var featureName = GitHelper.GetCurrentBranchName(true);
            EnvHelper.GetGitConfig();

            /* 1. Switch to the main feature branch
             * 2. Pull latest changes on main feature branch
             * 3. Merge the feature branch to main feature branch
             * 4. Push all changes to main feature branch
             * 5. Delete the local feature branch
             * 6. Delete the remote feature branch
             */
            string mainFeatureBranchFull = GitHelper.GetCurrentBranchName(false);
            string mainFeatureBranchName = mainFeatureBranchFull
                .Substring(mainFeatureBranchFull.LastIndexOf('/'));
            string mainFeatureBranchPrefix = mainFeatureBranchFull
                .Substring(0, mainFeatureBranchFull.Length - mainFeatureBranchName.Length);
            string mainFeatureBranch = $"{mainFeatureBranchPrefix}/{GitHelper.mainFeatureBranchSuffix}";

            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {mainFeatureBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"merge --no-ff {featureBranch}", false),
                $"Finishing feature-branch into main: {featureName}",
                featureBranch, null, _options, FormatCliCommand($"push origin {mainFeatureBranch}")
            );
        }

        private string FormatCliCommand(string gitCommand, bool appendNextLine = true)
        {
            return $"echo ^> {Path.GetFileNameWithoutExtension(EnvHelper.Git)} {gitCommand} && \"{EnvHelper.Git}\" {gitCommand}{(appendNextLine ? " && " : string.Empty)}";
        }

        private void FinishFeatureGitHubCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var featureBranch = GitHelper.GetCurrentBranchName(false);
            var featureName = GitHelper.GetCurrentBranchName(true);

            /* 1. Switch to the master branch
             * 2. Pull latest changes on master
             * 3. Merge the feature branch to master
             * 4. Push all changes to master
             * 5. Delete the local feature branch
             * 6. Delete the remote feature branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand("checkout master") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"merge --no-ff {featureBranch}", false),
                $"Finishing feature {featureName}",
                featureBranch, null, _options, FormatCliCommand("push origin master"));
        }

        private void StartReleaseCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var releaseVersion = Interaction.InputBox("Release Version:", "Start New Release");
            if (string.IsNullOrEmpty(releaseVersion)) return;

            var flowOptions = GitHelper.GetGitConfig();

            /* 1. Switch to the develop branch
             * 2. Pull latest changes on develop
             * 3. Create and switch to a new release branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {flowOptions.DevelopBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"checkout -b {flowOptions.ReleasePrefix}{releaseVersion} {flowOptions.DevelopBranch}", false),
                $"Starting release {releaseVersion}"
            );
        }

        private void FinishReleaseCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var releaseBranch = GitHelper.GetCurrentBranchName(false);
            var releaseName = GitHelper.GetCurrentBranchName(true);
            EnvHelper.GetGitConfig();

            /* 1. Switch to the master branch
             * 2. Pull latest changes on master
             * 3. Merge the release branch to master
             * 4. Tag the release
             * 5. Switch to the develop branch
             * 6. Pull latest changes on develop
             * 7. Merge the release branch to develop
             * 8. Push all changes to develop
             * 9. Push all changes to master
             * 10. Push the tag
             * 11. Delete the local release branch
             * 12. Delete the remote release branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {EnvHelper.GitConfig.MasterBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"merge --no-ff {releaseBranch}") +
                    FormatCliCommand($"tag {EnvHelper.GitConfig.TagPrefix}{releaseName}") +
                    FormatCliCommand($"checkout {EnvHelper.GitConfig.DevelopBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"merge --no-ff {releaseBranch}", false),
                $"Finishing release {releaseName}",
                releaseBranch, null, _options,
                    FormatCliCommand($"push origin {EnvHelper.GitConfig.DevelopBranch}") +
                    FormatCliCommand($"push origin {EnvHelper.GitConfig.MasterBranch}") +
                    FormatCliCommand($"push origin {EnvHelper.GitConfig.TagPrefix}{releaseName}")
            );
        }

        private void StartHotfixCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var hotfixVersion = Interaction.InputBox("Hotfix Version:", "Start New Hotfix");
            if (string.IsNullOrEmpty(hotfixVersion)) return;

            var flowOptions = GitHelper.GetGitConfig();

            /* 1. Switch to the master branch
             * 2. Pull latest changes on master
             * 3. Create and switch to a new hotfix branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {flowOptions.MasterBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"checkout -b {flowOptions.HotfixPrefix}{hotfixVersion} {flowOptions.MasterBranch}", false),
                $"Starting hotfix {hotfixVersion}"
            );
        }

        private void FinishHotfixCommand(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(EnvHelper.SolutionDir)) return;
            var hotfixBranch = GitHelper.GetCurrentBranchName(false);
            var hotfixName = GitHelper.GetCurrentBranchName(true);
            EnvHelper.GetGitConfig();

            /* 1. Switch to the master branch
             * 2. Pull latest changes on master
             * 3. Merge the hotfix branch to master
             * 4. Tag the hotfix
             * 5. Switch to the develop branch
             * 6. Pull latest changes on develop
             * 7. Merge the hotfix branch to develop
             * 8. Push all changes to develop
             * 9. Push all changes to master
             * 10. Push the tag
             * 11. Delete the local hotfix branch
             * 12. Delete the remote hotfix branch
             */
            ProcessHelper.StartProcessGui(
                "cmd.exe",
                $"/c cd \"{EnvHelper.SolutionDir}\" && " +
                    GitHelper.GetSshSetup() +
                    FormatCliCommand($"checkout {EnvHelper.GitConfig.MasterBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"merge --no-ff {hotfixBranch}") +
                    FormatCliCommand($"tag {EnvHelper.GitConfig.TagPrefix}{hotfixName}") +
                    FormatCliCommand($"checkout {EnvHelper.GitConfig.DevelopBranch}") +
                    FormatCliCommand("pull") +
                    FormatCliCommand($"merge --no-ff {hotfixBranch}", false),
                $"Finishing hotfix {hotfixName}",
                hotfixBranch, null, _options,
                    FormatCliCommand($"push origin {EnvHelper.GitConfig.DevelopBranch}") +
                    FormatCliCommand($"push origin {EnvHelper.GitConfig.MasterBranch}") +
                    FormatCliCommand($"push origin {EnvHelper.GitConfig.TagPrefix}{hotfixName}")
            );
        }
    }
}
