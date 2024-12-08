using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;

namespace IsoVistGH
{
    public class CurveTessellation : BaseComponent
    {
        /// <summary>
        /// Initializes a new instance of the CurveTessellation class.
        /// </summary>
        public CurveTessellation()
            : base("Curve Tesselation",
                 "CrvTesselation",
                 "Controlled tesselation of curved segments of a curve",
                 Properties.Resources.icon_question) {
        }

        public override void AddedToDocument(GH_Document document) {
            //Be sure that the value in Params.Input[value] is the index of the input you want to place the number slider
            if (Params.Input[1].SourceCount == 0) {
                // Perform Layout to get actual positionning of the component on the canvas
                Attributes.ExpireLayout();
                Attributes.PerformLayout();

                //instantiate new number slider
                var tesselations = new GH_NumberSlider();
                tesselations.CreateAttributes();

                // place the objects
                document.AddObject(tesselations, false);

                // plug the number slider to the GH component
                Params.Input[1].AddSource(tesselations);
                tesselations.Slider.Minimum = 1;
                tesselations.Slider.Maximum = 10;
                tesselations.Slider.DecimalPlaces = 0;
                tesselations.SetSliderValue(5);

                //get the pivot of the number slider
                PointF currPivot = Params.Input[1].Attributes.Pivot;

                //set the pivot of the new object
                tesselations.Attributes.Pivot = new PointF(currPivot.X - 228, currPivot.Y - 10);
            }

            base.AddedToDocument(document);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddCurveParameter("Curve", "Curve", "The curve to tesselater (if necessary)", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Divisions", "Divisions", "The number of divisions", GH_ParamAccess.item, 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddCurveParameter("Curve", "Curve", "The updated (tessealted) curve", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            if (!DA.TryGetItem(0, out Curve crv)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve input is not valid.");
                return;
            }

            if (!DA.TryGetItem(1, out int div_count)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The divisions input is not valid.");
                return;
            }

            List<int> curvedIds = Geometry.CurvedSegIDs(crv);
            Message = curvedIds.Count + "x curved segment(s)";
            DA.SetDataList(0, Geometry.TessellatePartOfCurve(crv.DuplicateSegments(), curvedIds, div_count));
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid {
            get { return new Guid("702C3568-22E0-4D7B-95A2-5A672DC4C259"); }
        }
    }
}