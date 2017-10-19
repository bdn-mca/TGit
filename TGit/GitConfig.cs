﻿using System.Linq;

namespace SamirBoulema.TGit
{
    public class GitConfig
    {
        public string MasterBranch;
        public string DevelopBranch;
        public string FeaturePrefix;
        public string ReleasePrefix;
        public string HotfixPrefix;
        public string TagPrefix;
        public string BugTraqMessage;
        public string LastFeatureBranchFullName;

        public GitConfig()
        {
            
        }

        public GitConfig(string input)
        {
            MasterBranch = string.Empty;
            DevelopBranch = string.Empty;
            FeaturePrefix = string.Empty;
            ReleasePrefix = string.Empty;
            HotfixPrefix = string.Empty;
            TagPrefix = string.Empty;

            foreach (var line in input.Split(';'))
            {
                if (line.StartsWith("gitflow.branch.master"))
                {
                    MasterBranch = line.Split(' ').Last();
                }
                else if (line.StartsWith("gitflow.branch.develop"))
                {
                    DevelopBranch = line.Split(' ').Last();
                }
                else if (line.StartsWith("gitflow.prefix.feature"))
                {
                    FeaturePrefix = line.Split(' ').Last();
                }
                else if (line.StartsWith("gitflow.prefix.release"))
                {
                    ReleasePrefix = line.Split(' ').Last();
                }
                else if (line.StartsWith("gitflow.prefix.hotfix"))
                {
                    HotfixPrefix = line.Split(' ').Last();
                }
                else if (line.StartsWith("gitflow.prefix.versiontag"))
                {
                    TagPrefix = line.Split(' ').Last();
                }
                else if (line.StartsWith("bugtraq.message"))
                {
                    BugTraqMessage = line.Split(' ').Last();
                }
                else if (line.StartsWith("gitflow.featbranch.last"))
                {
                    LastFeatureBranchFullName = line.Split(' ').Last();
                }
            }
        }
    }
}
