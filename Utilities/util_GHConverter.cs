using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System.Collections.Generic;

namespace IsoVistGH {
    public static class GH_Converter {
        #region Generic Conversion of types from GH to C#
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GENERIC CONVERSION OF TYPES from GH to C#

        /// <summary>
        /// Extension of Grasshopper embedded cast in order to include custom classes.
        /// </summary>
        public static bool TryGetItem<T>(this IGH_DataAccess DA, int paramID, out T item) {
            object obj = null;
            item = default(T);
            if (!DA.GetData(paramID, ref obj)) return false;
            try {
                if (GH_Convert.ToGoo(obj).CastTo(out T as_T)) item = as_T;
                else return false;
            }
            catch { }
            return true;
        }
        public static bool TryGetList<T>(this IGH_DataAccess DA, int paramID, out List<T> list) {
            list = new List<T>();
            if (!DA.GetDataList(paramID, list)) return false;
            return true;
        }
        public static bool TryGetTree<T>(this IGH_DataAccess DA, int paramID, out DataTree<T> tree) {
            tree = new DataTree<T>();
            if (!DA.GetDataTree(paramID, out GH_Structure<IGH_Goo> gooTree)) return false;
            try {
                GH_Path path = new GH_Path(0);
                for (int i = 0; i < gooTree.Branches.Count; i++) {
                    for (int j = 0; j < gooTree.Branches[i].Count; j++) {
                        if (!GH_Convert.ToGoo(gooTree[i][j]).CastTo(out T as_T)) return false;
                        tree.Add(as_T, path);
                    }
                    path = path.Increment(0);
                }
            }
            catch { }
            return true;
        }
        #endregion
    }
}