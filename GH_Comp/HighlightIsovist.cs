using System;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace IsoVistGH
{
    public class HighlightIsovist : HighlightCurveComponent {
        /// <summary>
        /// Initializes a new instance of the HighlightIsovist class.
        /// </summary>
        public HighlightIsovist()
          : base("Highlight Isovist",
                 "HighlightIsovist",
                 "Highlighted visualization of IsoVist polygon",
                 Properties.Resources.icon_question)
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("IsoVist", "IsoVist", "IsoVist to visualize", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Segments' Thickness", "Thickness", "The segments' thickness", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("Hatch scale", "HatchScale", "The hatch scale", GH_ParamAccess.item, 3.0);
            pManager.AddIntegerParameter("Hatch rotation", "HatchRotation", "The hatch rotation (in degrees", GH_ParamAccess.item, 45);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
            pManager[1].WireDisplay = GH_ParamWireDisplay.faint;
            pManager[2].WireDisplay = GH_ParamWireDisplay.faint;
            pManager[3].WireDisplay = GH_ParamWireDisplay.faint;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            if (!DA.TryGetItem(0, out Curve isovist)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The IsoVist polygon is not valid. Nothing to visualize");
                return;
            }

            DA.TryGetItem(1, out int seg_thickness);
            DA.TryGetItem(2, out double hatch_scale);
            DA.TryGetItem(3, out int hatch_rotation);

            painter.Init();
            painter.BuildPolygon(isovist, seg_thickness, hatch_scale, hatch_rotation * Math.PI / 180);
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid("04E9FA0F-BF9F-4C73-BC47-06FA9210CFAF"); }
        }
    }
}