﻿using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace AgOpenGPS
{
    public class CTurn
    {
        //copy of the mainform address
        private readonly FormGPS mf;

        private readonly double boxLength, scanWidth;

        /// <summary>
        /// array of turns
        /// </summary>
        public CTurnLines[] turnArr;

        //constructor
        public CTurn(FormGPS _f)
        {
            mf = _f;
            turnSelected = 0;
            scanWidth = 1.5;
            boxLength = 2000;

            //Turns array
            turnArr = new CTurnLines[FormGPS.MAXBOUNDARIES];
            for (int j = 0; j < FormGPS.MAXBOUNDARIES; j++) turnArr[j] = new CTurnLines();
        }

        // the list of possible bounds points
        public List<vec4> turnClosestList = new List<vec4>();

        public int turnSelected, closestTurnNum;

        //generated box for finding closest point
        public vec2 boxA = new vec2(9000, 9000), boxB = new vec2(9000, 9002);

        public vec2 boxC = new vec2(9001, 9001), boxD = new vec2(9002, 9003);

        //point at the farthest turn segment from pivotAxle
        public vec3 closestTurnPt = new vec3(-10000, -10000, 9);
        public vec3 closestTurnPt2 = new vec3(-10000, -10000, 9);

        public void ResetTurnLines()
        {
            for (int i = 0; i < FormGPS.MAXBOUNDARIES; i++)
                turnArr[i].ResetTurn();
        }

        public void FindClosestTurnPoint(bool isYouTurnRight, vec3 fromPt, double headAB)
        {
            //initial scan is straight ahead of pivot point of vehicle to find the right turnLine/boundary

            //isYouTurnRight actuall means turning left - Painful, but it switches later
            boxA.easting = fromPt.easting + (Math.Sin(headAB + glm.PIBy2) * -scanWidth);
            boxA.northing = fromPt.northing + (Math.Cos(headAB + glm.PIBy2) * -scanWidth);

            boxB.easting = fromPt.easting + (Math.Sin(headAB + glm.PIBy2) * scanWidth);
            boxB.northing = fromPt.northing + (Math.Cos(headAB + glm.PIBy2) * scanWidth);

            boxC.easting = boxB.easting + (Math.Sin(headAB) * boxLength);
            boxC.northing = boxB.northing + (Math.Cos(headAB) * boxLength);

            boxD.easting = boxA.easting + (Math.Sin(headAB) * boxLength);
            boxD.northing = boxA.northing + (Math.Cos(headAB) * boxLength);

            int ptCount, i;

            //determine if point is inside bounding box
            turnClosestList.Clear();
            vec4 inBox;
            for (i = 0; i < FormGPS.MAXHEADS; i++)
            {
                //skip the drive thru
                if (mf.bnd.bndArr[i].isDriveThru) continue;

                ptCount = turnArr[i].turnLine.Count;
                for (int p = 0; p < ptCount; p++)
                {
                    if ((((boxB.easting - boxA.easting) * (turnArr[i].turnLine[p].northing - boxA.northing))
                            - ((boxB.northing - boxA.northing) * (turnArr[i].turnLine[p].easting - boxA.easting))) < 0) { continue; }

                    if ((((boxD.easting - boxC.easting) * (turnArr[i].turnLine[p].northing - boxC.northing))
                            - ((boxD.northing - boxC.northing) * (turnArr[i].turnLine[p].easting - boxC.easting))) < 0) { continue; }

                    if ((((boxC.easting - boxB.easting) * (turnArr[i].turnLine[p].northing - boxB.northing))
                            - ((boxC.northing - boxB.northing) * (turnArr[i].turnLine[p].easting - boxB.easting))) < 0) { continue; }

                    if ((((boxA.easting - boxD.easting) * (turnArr[i].turnLine[p].northing - boxD.northing))
                            - ((boxA.northing - boxD.northing) * (turnArr[i].turnLine[p].easting - boxD.easting))) < 0) { continue; }

                    //it's in the box, so add to list
                    inBox.easting = turnArr[i].turnLine[p].easting;
                    inBox.northing = turnArr[i].turnLine[p].northing;
                    inBox.heading = turnArr[i].turnLine[p].heading;
                    inBox.index = i;

                    //which turn/headland is it from
                    turnClosestList.Add(inBox);
                }
            }

            //which of the points is closest
            closestTurnPt.easting = -20000; closestTurnPt.northing = -20000;
            ptCount = turnClosestList.Count;
            if (ptCount != 0)
            {
                //determine closest point
                double minDistance = 9999999;
                for (i = 0; i < ptCount; i++)
                {
                    double dist = ((fromPt.easting - turnClosestList[i].easting) * (fromPt.easting - turnClosestList[i].easting))
                                    + ((fromPt.northing - turnClosestList[i].northing) * (fromPt.northing - turnClosestList[i].northing));
                    if (minDistance >= dist)
                    {
                        minDistance = dist;

                        closestTurnPt.easting = turnClosestList[i].easting;
                        closestTurnPt.northing = turnClosestList[i].northing;
                        closestTurnPt.heading = turnClosestList[i].heading;
                        mf.turn.closestTurnNum = (int)turnClosestList[i].index;
                    }
                }
                if (closestTurnPt.heading < 0) closestTurnPt.heading += glm.twoPI;
            }

            //second scan is straight ahead of outside of tool based on turn direction
            double scanWidthL, scanWidthR;
            if (isYouTurnRight) //its actually left
            {
                scanWidthL = -scanWidth - (mf.vehicle.toolWidth * 0.5);
                scanWidthR = scanWidth - (mf.vehicle.toolWidth * 0.5);
            }
            else
            {
                scanWidthL = -scanWidth + (mf.vehicle.toolWidth * 0.5);
                scanWidthR = scanWidth + (mf.vehicle.toolWidth * 0.5);
            }

            //isYouTurnRight actuall means turning left - Painful, but it switches later
            boxA.easting = fromPt.easting + (Math.Sin(headAB + glm.PIBy2) * scanWidthL);
            boxA.northing = fromPt.northing + (Math.Cos(headAB + glm.PIBy2) * scanWidthL);

            boxB.easting = fromPt.easting + (Math.Sin(headAB + glm.PIBy2) * scanWidthR);
            boxB.northing = fromPt.northing + (Math.Cos(headAB + glm.PIBy2) * scanWidthR);

            boxC.easting = boxB.easting + (Math.Sin(headAB) * boxLength);
            boxC.northing = boxB.northing + (Math.Cos(headAB) * boxLength);

            boxD.easting = boxA.easting + (Math.Sin(headAB) * boxLength);
            boxD.northing = boxA.northing + (Math.Cos(headAB) * boxLength);

            //determine if point is inside bounding box of the 1 turn chosen above
            turnClosestList.Clear();

            i = mf.turn.closestTurnNum;

            ptCount = turnArr[i].turnLine.Count;
            for (int p = 0; p < ptCount; p++)
            {
                if ((((boxB.easting - boxA.easting) * (turnArr[i].turnLine[p].northing - boxA.northing))
                        - ((boxB.northing - boxA.northing) * (turnArr[i].turnLine[p].easting - boxA.easting))) < 0) { continue; }

                if ((((boxD.easting - boxC.easting) * (turnArr[i].turnLine[p].northing - boxC.northing))
                        - ((boxD.northing - boxC.northing) * (turnArr[i].turnLine[p].easting - boxC.easting))) < 0) { continue; }

                if ((((boxC.easting - boxB.easting) * (turnArr[i].turnLine[p].northing - boxB.northing))
                        - ((boxC.northing - boxB.northing) * (turnArr[i].turnLine[p].easting - boxB.easting))) < 0) { continue; }

                if ((((boxA.easting - boxD.easting) * (turnArr[i].turnLine[p].northing - boxD.northing))
                        - ((boxA.northing - boxD.northing) * (turnArr[i].turnLine[p].easting - boxD.easting))) < 0) { continue; }

                //it's in the box, so add to list
                inBox.easting = turnArr[i].turnLine[p].easting;
                inBox.northing = turnArr[i].turnLine[p].northing;
                inBox.heading = turnArr[i].turnLine[p].heading;
                inBox.index = i;

                //which turn/headland is it from
                turnClosestList.Add(inBox);
            }

            //which of the points is closest
            //closestTurnPt.easting = -20000; closestTurnPt.northing = -20000;
            ptCount = turnClosestList.Count;
            if (ptCount != 0)
            {
                //determine closest point
                double minDistance = 9999999;
                for (i = 0; i < ptCount; i++)
                {
                    double dist = ((fromPt.easting - turnClosestList[i].easting) * (fromPt.easting - turnClosestList[i].easting))
                                    + ((fromPt.northing - turnClosestList[i].northing) * (fromPt.northing - turnClosestList[i].northing));
                    if (minDistance >= dist)
                    {
                        minDistance = dist;

                        closestTurnPt2.easting = turnClosestList[i].easting;
                        closestTurnPt2.northing = turnClosestList[i].northing;
                        closestTurnPt2.heading = turnClosestList[i].heading;
                    }
                }
                if (closestTurnPt2.heading < 0) closestTurnPt.heading += glm.twoPI;

                //point must be in the neighborhood of center point
                if (glm.Distance(closestTurnPt2.easting, closestTurnPt2.northing, closestTurnPt.easting, closestTurnPt.northing) < mf.vehicle.toolWidth)
                {
                    closestTurnPt.easting = closestTurnPt2.easting;
                    closestTurnPt.northing = closestTurnPt2.northing;
                    closestTurnPt.heading =  closestTurnPt2.heading;
                }
            }
        }

        public void BuildTurnLines()
        {
            if (!mf.bnd.bndArr[0].isSet)
            {
                mf.TimedMessageBox(1500, " Error", "No Boundaries Made");
                return;
            }

            //to fill the list of line points
            vec3 point = new vec3();

            //determine how wide a headland space
            double totalHeadWidth = mf.yt.triggerDistanceOffset;

            //outside boundary - count the points from the boundary
            turnArr[0].turnLine.Clear();
            int ptCount = mf.bnd.bndArr[0].bndLine.Count;
            for (int i = ptCount - 1; i >= 0; i--)
            {
                //calculate the point inside the boundary
                point.easting = mf.bnd.bndArr[0].bndLine[i].easting + (-Math.Sin(glm.PIBy2 + mf.bnd.bndArr[0].bndLine[i].heading) * totalHeadWidth);
                point.northing = mf.bnd.bndArr[0].bndLine[i].northing + (-Math.Cos(glm.PIBy2 + mf.bnd.bndArr[0].bndLine[i].heading) * totalHeadWidth);
                point.heading = mf.bnd.bndArr[0].bndLine[i].heading;
                if (point.heading < -glm.twoPI) point.heading += glm.twoPI;

                //only add if inside actual field boundary
                if (mf.bnd.bndArr[0].IsPointInsideBoundary(point))
                {
                    CTurnPt tPnt = new CTurnPt(point.easting, point.northing, point.heading);
                    turnArr[0].turnLine.Add(tPnt);
                }
            }
            turnArr[0].FixTurnLine(totalHeadWidth, mf.bnd.bndArr[0].bndLine);
            turnArr[0].PreCalcTurnLines();

            //inside boundaries
            for (int j = 1; j < FormGPS.MAXBOUNDARIES; j++)
            {
                turnArr[j].turnLine.Clear();
                if (!mf.bnd.bndArr[j].isSet || mf.bnd.bndArr[j].isDriveThru) continue;

                ptCount = mf.bnd.bndArr[j].bndLine.Count;

                for (int i = ptCount - 1; i >= 0; i--)
                {
                    //calculate the point outside the boundary
                    point.easting = mf.bnd.bndArr[j].bndLine[i].easting + (-Math.Sin(glm.PIBy2 + mf.bnd.bndArr[j].bndLine[i].heading) * totalHeadWidth);
                    point.northing = mf.bnd.bndArr[j].bndLine[i].northing + (-Math.Cos(glm.PIBy2 + mf.bnd.bndArr[j].bndLine[i].heading) * totalHeadWidth);
                    point.heading = mf.bnd.bndArr[j].bndLine[i].heading;
                    if (point.heading < -glm.twoPI) point.heading += glm.twoPI;

                    //only add if outside actual field boundary
                    if (!mf.bnd.bndArr[j].IsPointInsideBoundary(point))
                    {
                        CTurnPt tPnt = new CTurnPt(point.easting, point.northing, point.heading);
                        turnArr[j].turnLine.Add(tPnt);
                    }
                }
                turnArr[j].FixTurnLine(totalHeadWidth, mf.bnd.bndArr[j].bndLine);
                turnArr[j].PreCalcTurnLines();
            }

            mf.TimedMessageBox(800, "Turn Lines", "Turn limits Created");
        }

        public void DrawTurnLines()
        {
            for (int i = 0; i < FormGPS.MAXBOUNDARIES; i++)
            {
                if (mf.bnd.bndArr[i].isSet)
                    turnArr[i].DrawTurnLine();
            }
        }

        //draws the derived closest point
        public void DrawClosestPoint()
        {
            GL.PointSize(4.0f);
            GL.Color3(0.219f, 0.932f, 0.070f);
            GL.Begin(PrimitiveType.Points);
            GL.Vertex3(closestTurnPt.easting, closestTurnPt.northing, 0);
            GL.End();

            //GL.LineWidth(1);
            //GL.Color3(0.92f, 0.62f, 0.42f);
            //GL.Begin(PrimitiveType.LineStrip);
            //GL.Vertex3(boxD.easting, boxD.northing, 0);
            //GL.Vertex3(boxA.easting, boxA.northing, 0);
            //GL.Vertex3(boxB.easting, boxB.northing, 0);
            //GL.Vertex3(boxC.easting, boxC.northing, 0);
            //GL.End();
        }
    }
}