using System;
namespace TechCosmos.InitializeSortSystem.Runtime
{
    public class InitializeData
    {
        public int SortLevel;
        public Action InitializeAction;
        public InitializeData(Action InitializeAction, int SortLevel)
        {
            this.InitializeAction = InitializeAction;
            this.SortLevel = SortLevel;
        }
    }
}