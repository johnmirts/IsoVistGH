using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace IsoVistGH
{
    public class IsoVist2D : BaseComponent
    {
        /// <summary>
        /// Initializes a new instance of the IsoVist2DMyComponent1 class.
        /// </summary>
        public IsoVist2D()
          : base("IsoVist 2D",
                 "IsoVist2D",
                 "Calculate the isovist polygon",
                 GH_Exposure.primary,
                 Properties.Resources.icon_question)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddCurveParameter("Regions", "C", "The regions for which the isovist polygon is calculated", GH_ParamAccess.list);
            pManager.AddPointParameter("Point", "P", "The point of view", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddCurveParameter("IsoVist polygon", "P", "The IsoVist 2D polygon", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {

            #region INPUT
            if (!DA.TryGetList(0, out List<Curve> crvs)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curves input is not valid.");
                return;
            }

            if (!DA.TryGetItem(1, out Point3d pt)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The point input is not valid.");
                return;
            }
            #endregion

            #region WARN ABOUT CURVATURE
            int curved_segs = 0;
            foreach (Curve crv in crvs) { curved_segs += Geometry.NoCurvedSegs(crv); }
            Message += "\n" + curved_segs.ToString() + "x curved segment(s)";
            if (curved_segs > 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There are " + curved_segs.ToString() + "x curved segment(s) in the provided curves. The accuracy of the query is not ensured. Reconsider and/or try to tesselate the relevant curves. Use the \"Tesselation\" provided component if necessary.");
            }
            #endregion

            #region CALCULATE ISOVIST
            Curve masterRegion = new PolyCurve();
            Curve[] voidRegions = new Curve[] { };
            if (crvs.Count == 0) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There are no curves to calculate the isovist.");
                return;
            }
            else if (crvs.Count == 1) {
                masterRegion = crvs[0];
                PointContainment config = masterRegion.Contains(pt, masterRegion.PlaneFromRegion(), Geometry.Tolerance);
                if (config == PointContainment.Outside || config == PointContainment.Unset) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The given point is not inside the closed breps");
                }
                else {
                    DA.SetDataList(0, Visibility.IsoVist2D(masterRegion, pt));
                }
            }
            else {
                var temp = crvs.ToArray().MasterRegionVoids();
                masterRegion = temp.Item1;
                voidRegions = temp.Item2;

                DA.SetDataList(0, Visibility.IsoVist2D(masterRegion, voidRegions, pt));
            }
            #endregion
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid("F99E201D-4C47-48DF-B597-706DA125FD8A"); }
        }
    }
}