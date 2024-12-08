using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace IsoVistGH {
    public static class Visibility {
        private static readonly double Tolerance = Geometry.Tolerance;

        #region CONSTRUCTION METHODS
        // VISIBILITY SPECIFIC CONSTRUCTION METHODS
        /// <summary>
        /// Define a triangular Brep surface from an edge and a point in space.
        /// </summary>
        /// <param name="pt">
        /// The "head" point of the triangular Brep.
        /// </param>
        /// <param name="segment">
        /// The "base" edge of the triangular Brep surface.
        /// </param>
        /// <returns>
        /// A triangular Brep surface.
        /// </returns>
        private static Point3d[] _triangularRegionPts(Point3d pt, Curve segment, double length) {
            Vector3d pt2 = new Vector3d(segment.PointAtStart) - new Vector3d(pt);
            Vector3d pt3 = new Vector3d(segment.PointAtEnd) - new Vector3d(pt);
            pt2.Unitize();
            pt3.Unitize();
            Point3d s = new Point3d(pt.X + pt2.X * length, pt.Y + pt2.Y * length, pt.Z + pt2.Z * length);
            Point3d e = new Point3d(pt.X + pt3.X * length, pt.Y + pt3.Y * length, pt.Z + pt3.Z * length);
            return new Point3d[] { s, e };
        }
        private static Curve _triangularRegion(Point3d pt, Point3d s, Point3d e) {
            Curve[] curves = new LineCurve[] { new LineCurve(pt, s), new LineCurve(s, e), new LineCurve(e, pt) };
            return Curve.JoinCurves(curves, Tolerance)[0];
        }
        #endregion

        #region ISOVIST 2D
        private static Curve[] _isoVist2D_(Curve masterArea, Curve[] voidPolygons, Point3d pt) {
            // Get some general information
            // Get the BoundingBox of this polygon ((bbox))
            var bbox = masterArea.PolygonBoundingBoxProperties();

            // Get the diagonal brep's diagonal length (length)
            double length = 1.1 * bbox.Item2;

            // Get the normal of the plane where all polygons are
            Plane plane = masterArea.PlaneFromRegion();
            Vector3d normal = plane.ZAxis;

            Curve[] area1 = new Curve[] { };
            Curve[] area2 = new Curve[] { };
            Curve[] area3 = new Curve[] { };

            #region STEP #1: find the visibility domain as polygon(s) (area1) without considering the voids
            List<Curve> unseenAreas = new List<Curve>();
            foreach (Curve edge in masterArea.DuplicateSegments()) {
                Point3d s = edge.PointAtStart;
                Point3d e = edge.PointAtEnd;
                Point3d m = new Point3d((s.X + e.X) / 2, (s.Y + e.Y) / 2, (s.Z + e.Z) / 2);

                if (!Geometry.IsPtOnCurve(pt, edge)) {
                    // build a curve between m and pt
                    // check if this crv intersects masterArea (Curve) more than once
                    // if yes, build an "infinite triangle"
                    Curve crv = new LineCurve(pt, m);
                    // check if crv intersects the masterArea more than once
                    List<Point3d> intPts = new List<Point3d>();
                    CurveIntersections iEvent = Intersection.CurveCurve(crv, masterArea, Tolerance, Tolerance);
                    for (int j = 0; j < iEvent.Count; j++) {
                        Point3d a = iEvent[j].PointA;
                        if (a.DistanceTo(iEvent[j].PointB) < Tolerance) {
                            intPts.Add(a);
                        }
                    }

                    if (intPts.Count > 1) {
                        // if crv intersects the masterArea more than once, build an "infinite triangle"
                        Point3d[] ptsInf = _triangularRegionPts(pt, edge, length);
                        Curve triangle = _triangularRegion(pt, ptsInf[0], ptsInf[1]);
                        Curve[] split = triangle.Split(Surface.CreateExtrusion(edge, normal), Tolerance, Tolerance);
                        if (split.Length > 0) {
                            foreach (Curve c in split) {
                                c.ClosestPoint(pt, out double t);
                                if (pt.DistanceTo(c.PointAt(t)) > Tolerance) {
                                    unseenAreas.AddRange(Curve.JoinCurves(new Curve[] { c, new LineCurve(c.PointAtStart, c.PointAtEnd) }, Tolerance));
                                    break;
                                }
                            }
                        }
                    }
                }
            }


            // get area1
            if (unseenAreas.Count > 0) {
                List<Curve> valid = new List<Curve>();
                foreach (Curve crv in unseenAreas) { if (crv.IsValid) { valid.Add(crv); } }
                Curve[] union = Curve.CreateBooleanUnion(valid, Tolerance);
                if (union.Length > 0) {
                    area1 = Geometry.DifferenceRegionRegion(new Curve[] { masterArea }, union, plane);
                }
                else {
                    area1 = Geometry.DifferenceRegionRegion(new Curve[] { masterArea }, valid.ToArray(), plane);
                }
            }
            else {
                area1 = new Curve[] { masterArea };
            }
            #endregion

            List<Curve> triangles = new List<Curve>();
            List<Curve> additionalTriangles = new List<Curve>();
            #region STEP #2: find the visibility domain as polygons(s) (area2) considering only the voids and their additional cones
            if (voidPolygons.Length > 0) {
                // iterate through each and every void (polygon)
                foreach (Curve polygon in voidPolygons) {
                    // Instatiate a list of curves where all (approved) intersection curves are stored
                    List<Curve> polygonTriangles = new List<Curve>();
                    foreach (LineCurve segment in polygon.DuplicateSegments()) {
                        Point3d[] ptsInf = _triangularRegionPts(pt, segment, length);
                        Curve triangle = _triangularRegion(pt, ptsInf[0], ptsInf[1]);
                        try {
                            // if the point is perfectly aligned with the edge then the triangle has zero area, and returns an error
                            double check = AreaMassProperties.Compute(triangle).Area;
                            polygonTriangles.Add(triangle);
                        }
                        catch {
                            // TODO
                        }
                    }
                    triangles.AddRange(Curve.CreateBooleanUnion(polygonTriangles, Tolerance));
                }

                Curve[] trianglesUnion = Curve.CreateBooleanUnion(triangles, Tolerance);
                if (trianglesUnion.Length == 0) { trianglesUnion = triangles.ToArray(); }

                foreach (Curve voidPolygon in voidPolygons) {
                    foreach (LineCurve segment in voidPolygon.DuplicateSegments()) {
                        Point3d m = segment.PointAtNormalizedLength(0.5);
                        // build a testCurve between m and pt
                        // check if this testCurve intersects polygon (void) only once
                        // if yes, build an "finite triangle"
                        Curve testCurve = new LineCurve(pt, m);
                        // check if crv intersects the polygon only once
                        CurveIntersections iEvent = Intersection.CurveCurve(testCurve, voidPolygon, Tolerance, Tolerance);
                        List<Point3d> intPts = new List<Point3d>();
                        for (int i = 0; i < iEvent.Count; i++) {
                            Point3d a = iEvent[i].PointA;
                            if (a.DistanceTo(iEvent[i].PointB) < Tolerance) {
                                intPts.Add(a);
                            }
                        }

                        if (intPts.Count == 1) {
                            // if testCurve intersects the polygon (void) only once, build an "finite & capped triangle"
                            Curve triangle = _triangularRegion(pt, segment.PointAtStart, segment.PointAtEnd);
                            try {
                                // if the point is perfectly aligned with the edge then the triangle has zero area, and returns an error
                                double check = AreaMassProperties.Compute(triangle).Area;
                                additionalTriangles.Add(triangle);
                            }
                            catch {

                            }
                        }
                    }
                }
                area2 = Geometry.DifferenceRegionRegion(new Curve[] { masterArea }, trianglesUnion, plane);
                area3 = Curve.CreateBooleanUnion(additionalTriangles, Tolerance);
            }
            #endregion

            if (voidPolygons.Length == 0) {
                return area1;
            }

            List<Curve> area4 = area3.ToList();
            area2.ToList();
            area4.AddRange(area2);
            Curve[] regions = Curve.CreateBooleanUnion(area4, Tolerance);
            Curve[] final = Curve.CreateBooleanIntersection(area1[0], regions[0], Tolerance);
            int iter = 100;
            while (final.Length == 0 && iter <= 5000) {

                final = Curve.CreateBooleanIntersection(area1[0].Simplify(CurveSimplifyOptions.All, iter * Tolerance, iter * Tolerance),
                                                        regions[0].Simplify(CurveSimplifyOptions.All, iter * Tolerance, iter * Tolerance),
                                                        iter * Tolerance);
                iter += 100;
            }
            return final;
        }
        #endregion

        #region ISOVIST FINAL
        /// <summary>
        /// Calculate the Isovist for a (closed) region without voids
        /// </summary>
        /// <param name="masterArea">
        /// The (closed) region without voids to evaluate.
        /// </param>
        /// <param name="pt">
        /// The point the Isovist is calculated for.
        /// </param>
        /// <returns>
        /// An array of (closed) regions that have uninterrupted view from the provided point.
        /// </returns>
        public static Curve[] IsoVist2D(Curve masterArea, Point3d pt) {
            return _isoVist2D_(masterArea, new Curve[] { }, pt);
        }
        /// <summary>
        /// Calculate the Isovist for a (closed) region with voids.
        /// </summary>
        /// <param name="masterArea">
        /// The (closed) region which contains all the voids.
        /// </param>
        /// <param name="voidPolygons">
        /// The voids of the (closed) region the Isovist is calculated for.
        /// </param>
        /// <param name="pt">
        /// The point the Isovist is calculated for.
        /// </param>
        /// <returns>
        /// An array of (closed) regions that have uninterrupted view from the provided point.
        /// </returns>
        public static Curve[] IsoVist2D(Curve masterArea, Curve[] voidPolygons, Point3d pt) {
            return _isoVist2D_(masterArea, voidPolygons, pt);
        }
        #endregion
    }
}