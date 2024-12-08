using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IsoVistGH {
    internal static class Geometry {
        internal static readonly double Tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

        //////////////////////////////////////////////////////////
        /// PRIVATE METHODS
        //////////////////////////////////////////////////////////

        /// <summary>
        /// Check if two vectors are parallel to each other.
        /// </summary>
        /// <param name="vecA">
        /// The first vector.
        /// </param>
        /// <param name="vecB">
        /// The second vector.
        /// </param>
        /// <returns>
        /// True if the vectors are parallel, else False.
        /// </returns>
        private static bool _isParallel(Vector3d vecA, Vector3d vecB) {
            int parallelCheck = vecA.IsParallelTo(vecB, RhinoMath.DefaultAngleTolerance);
            if (parallelCheck == -1 || parallelCheck == 1) {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Check if a point is on a plane.
        /// </summary>
        /// <param name="pt">
        /// The point to be checked.
        /// </param>
        /// <param name="plane">
        /// The plance that is used as a reference.
        /// </param>
        /// <returns>
        /// True if poiknt is on the provided plane, else False.
        /// </returns>
        private static bool _isPtOnPlane(this Point3d pt, Plane plane) {
            double distance = plane.DistanceTo(pt);
            if (Math.Abs(distance) > Tolerance) {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Check if a set of (closed) regions all lie on the same plane.
        /// </summary>
        /// <param name="regions">
        /// The set of (closed) regions to be checked.
        /// </param>
        /// <returns>
        /// True if regions lie on the same plane, else False.
        /// </returns>
        private static bool _areCoplanar(this Curve[] regions) {
            foreach (Curve region in regions) {
                if (!region.IsPlanar()) { return false; }
            }

            Plane plane = regions[0].PlaneFromRegion();
            for (int i = 1; i < regions.Length; i++) {
                if (!regions[i].PointAtStart._isPtOnPlane(plane)) { return false; }
            }
            return true;

        }
        /// <summary>
        /// Check if two (closed) regions lie on the same plane.
        /// </summary>
        /// <param name="region1">
        /// The firs plane to check.
        /// </param>
        /// <param name="region2">
        /// The second plane to check.
        /// </param>
        /// <returns>
        /// True if regions lie on the same plane, else False.
        /// </returns>
        private static bool _areCoplanar(this Curve region1, Curve region2) {
            if (!region1.IsPlanar() || !region2.IsPlanar()) { return false; }
            Plane plane = region1.PlaneFromRegion();

            if (region2.PointAtStart._isPtOnPlane(plane)) { return true; }
            return false;
        }
        private static Curve _tesselateCurve(Curve seg, int div_count) {
            Point3d[] pts = new Point3d[div_count + 1];
            foreach (var kvp in (seg.DivideByCount(div_count, true)).Select((value, index) => new { value, index })) {
                pts[kvp.index] = seg.PointAt(kvp.value);
            }
            Polyline p = new Polyline(pts);
            return p.ToNurbsCurve();
        }

        //////////////////////////////////////////////////////////
        /// INTERNAL METHODS
        //////////////////////////////////////////////////////////


        /// <summary>
        /// Get the orthocanonical bounding box of a planar closed region.
        /// </summary>
        /// <param name="region">
        /// A planar closed region that is inscribed into a bounding box.
        /// </param>
        /// <returns>
        /// (a) The orthocanonical bounding box; (b) the length of the bounding box diagonal.
        /// </returns>
        internal static Tuple<Rectangle3d, double> PolygonBoundingBoxProperties(this Curve region) {
            Plane plane = region.PlaneFromRegion();
            double tmin = 1e8;
            double tmax = -1e8;
            double smin = 1e8;
            double smax = -1e8;
            foreach (Point3d pt in region.GetBoundingBox(false).GetCorners()) {
                bool check = plane.ClosestParameter(pt, out double s, out double t);
                if (tmin > t) { tmin = t; }
                if (tmax < t) { tmax = t; }
                if (smin > s) { smin = s; }
                if (smax < s) { smax = s; }
            }

            Point3d pt1 = plane.PointAt(smin, tmin);
            Point3d pt2 = plane.PointAt(smax, tmax);
            Rectangle3d rectangle = new Rectangle3d(plane, pt1, pt2);

            return new Tuple<Rectangle3d, double>(rectangle, pt1.DistanceTo(pt2));
        }
        /// <summary>
        /// Check if a point lies on a curve.
        /// </summary>
        /// <param name="pt">
        /// The point to be checked.
        /// </param>
        /// <param name="curve">
        /// The curve that is used as a reference.
        /// </param>
        /// <returns>
        /// True if point lies on the provided curve, else False.
        /// </returns>
        internal static bool IsPtOnCurve(this Point3d pt, Curve curve) {
            curve.ClosestPoint(pt, out double t);
            Point3d cp = curve.PointAt(t);
            if (pt.DistanceTo(cp) > Tolerance) {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Finds the segment IDs of a curve which are not linear (curved).
        /// </summary>
        /// <param name="crv">
        /// The curve to evaluate.
        /// </param>
        /// <returns>
        /// The segment IDs that are not linear.
        /// </returns>
        internal static List<int> CurvedSegIDs(Curve crv) {

            Curve[] segments = crv.DuplicateSegments();
            List<int> curvedSegIDs = new List<int>();
            foreach (var kvp in segments.Select((value, index) => new { value, index })) {
                Curve seg = segments[kvp.index];
                Vector3d guide_vec = new Vector3d(seg.PointAtEnd) - new Vector3d(seg.PointAtStart);

                double[] parameters = seg.DivideByCount(3, false);
                foreach (double param in parameters) {
                    Vector3d tangent = seg.TangentAt(param);
                    if (!_isParallel(guide_vec, tangent)) {
                        curvedSegIDs.Add(kvp.index);
                        break;
                    }
                }
            }
            return curvedSegIDs;
        }
        /// <summary>
        /// Find the region that contains the voids. Prerequisite: All regions lie on the same plane.
        /// </summary>
        /// <param name="regions">
        /// The (closed) regions to be checked.
        /// </param>
        /// <returns>
        /// (a) The "master" region; (b) The voids of the "master" region; (c) The plane where the (closed) regions lie.
        /// </returns>
        internal static Tuple<Curve, Curve[], Plane> MasterRegionVoids(this Curve[] regions) {
            // Each polygon in voids MUST be included  by the masterArea
            if (regions._areCoplanar()) {
                Curve masterArea;
                List<Curve> voids = new List<Curve>();
                Plane plane;
                if (regions.Length > 1) {
                    SortedList<double, Curve> SortedAreaPolygons = new SortedList<double, Curve>();
                    foreach (Curve polygon in regions) {
                        double area = AreaMassProperties.Compute(polygon).Area;
                        if (!SortedAreaPolygons.ContainsKey(area)) {
                            SortedAreaPolygons.Add(area, polygon);
                        }
                    }

                    // the polygon with the largest area is selected as the masterArea
                    masterArea = SortedAreaPolygons.Values[SortedAreaPolygons.Count - 1];
                    plane = masterArea.PlaneFromRegion();

                    if (masterArea.IsValid) {
                        // all other polygons contained in masterPolygon will be the voids
                        for (int i = 0; i < SortedAreaPolygons.Count - 1; i++) {
                            Curve polygon = SortedAreaPolygons.Values[i];
                            RegionContainment config = Curve.PlanarClosedCurveRelationship(masterArea, polygon, plane, Tolerance);
                            if (config == RegionContainment.BInsideA) {
                                voids.Add(polygon);
                            }
                        }
                    }
                    else {
                        // TODO
                        return null;
                    }
                }
                else {
                    masterArea = regions[0];
                    plane = masterArea.PlaneFromRegion();
                }
                return new Tuple<Curve, Curve[], Plane>(masterArea, voids.ToArray(), plane);
            }
            else {
                // TODO
                return null;
            }
        }
        /// <summary>
        /// Rethrn the number of curved segments that need to be tessellated,
        /// </summary>
        /// <param name="crv">
        /// The curve to evaluate
        /// </param>
        /// <returns>
        /// The number of curved segments.
        /// </returns>
        internal static int NoCurvedSegs(Curve crv) {
            return CurvedSegIDs(crv).Count;
        }
        /// <summary>
        /// Create a plane from a (closed) region
        /// </summary>
        /// <param name="region">
        /// The planar region to be evaluated.
        /// </param>
        /// <returns>
        /// The plane where the provided (closed) region lies on.
        /// </returns>
        internal static Plane PlaneFromRegion(this Curve region) {
            if (region.IsClosed && region.IsPlanar()) {
                Point3d pt1 = region.PointAtNormalizedLength(0);
                Point3d pt2 = region.PointAtNormalizedLength(0.33);
                Point3d pt3 = region.PointAtNormalizedLength(0.66);
                return new Plane(pt1, new Vector3d(pt2) - new Vector3d(pt1), new Vector3d(pt3) - new Vector3d(pt1));
            }
            else { return new Plane(); }
        }
        /// <summary>
        /// Get the boolean difference between two sets of (closed) regions lying on the same plane.
        /// </summary>
        /// <param name="regionsA">
        /// The sets of (closed) regions to remove from.
        /// </param>
        /// <param name="regionsB">
        /// The set of (closed) regions to remove with.
        /// </param>
        /// <param name="plane">
        /// The plane where the (closed) regions lie on.
        /// </param>
        /// <returns>
        /// An array of (closed) regions that arise from the boolean difference operation.
        /// </returns>
        internal static Curve[] DifferenceRegionRegion(Curve[] regionsA, Curve[] regionsB, Plane plane) {
            // regionsA are the regions to remove from
            // regionsB are the regions to remove with

            List<Curve> resultCurves = new List<Curve>();

            void BooleanDifferenceRecursive(Curve curve, List<Curve> subtractors) {
                foreach (Curve subtractorCurve in subtractors) {

                    if (curve._areCoplanar(subtractorCurve)) {
                        RegionContainment config = Curve.PlanarClosedCurveRelationship(curve, subtractorCurve, plane, Tolerance);
                        if (config == RegionContainment.MutualIntersection) {
                            Curve[] differenceCurves = Curve.CreateBooleanDifference(curve, subtractorCurve, Tolerance);

                            if (differenceCurves != null && differenceCurves.Length > 0) {
                                curve = differenceCurves[0];
                                if (differenceCurves.Length > 1) {
                                    // If extra curves are generated, recursively apply boolean difference
                                    for (int i = 1; i < differenceCurves.Length; i++) {
                                        BooleanDifferenceRecursive(differenceCurves[i], subtractors);
                                    }
                                }
                            }
                        }
                    }
                }
                resultCurves.Add(curve);
            }

            // Apply boolean difference to each curve in regionsA
            foreach (Curve crvA in regionsA) {
                BooleanDifferenceRecursive(crvA, regionsB.ToList());
            }

            return resultCurves.ToArray();
        }
        internal static Curve[] TessellatePartOfCurve(Curve[] segments, List<int> ids, int div_count) {
            Curve[] final_segs = new Curve[segments.Length];
            foreach (var kvp in segments.Select((value, index) => new { value, index })) {
                if (ids.Contains(kvp.index)) {
                    final_segs[kvp.index] = _tesselateCurve(kvp.value, div_count);
                }
                else {
                    final_segs[kvp.index] = segments[kvp.index];
                }
            }
            return Curve.JoinCurves(final_segs, Geometry.Tolerance);
        }
    }
}