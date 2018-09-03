using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace ApUtilitiesLib
{
    internal class ApUtilities
    {
        internal static void AtInfo()
        {
            Database db = null;
            Editor ed = null;
            Alignment al = null;
            List<List<double>> irregularEquations = null;
            
            ObjectId alignId = GetAlignment(ref db, ref ed);
            GetAlignmentProperties(db, alignId, ref al, ref irregularEquations);
            PrintAtInfo(ed, al, irregularEquations);
        }

        internal static void AtStation()
        {
            Database db = null;
            Editor ed = null;
            Alignment al = null;
            List<List<double>> irregularEquations = null;
            Document doc = null;

            ObjectId alignId = GetAlignment(ref db, ref ed);
            GetAlignmentProperties(db, alignId, ref al, ref irregularEquations);

            doc = ed.Document;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                double station = Double.NaN;
                double offset = Double.NaN;
                PromptPointOptions ptOpts = new PromptPointOptions("\nВыберите точку для отображения пикета и смещения:");
                ptOpts.AllowNone = true;
                ptOpts.AllowArbitraryInput = false;
                PromptPointResult ptRes = doc.Editor.GetPoint(ptOpts);

                while (ptRes.Status == PromptStatus.OK)
                {
                    try
                    {
                        al.StationOffset(ptRes.Value.X, ptRes.Value.Y, ref station, ref offset);

                        if (Math.Abs(station) <= 100 || !irregularEquations.Any())
                        {
                            PrintCurrentStationInfo(ed, station, offset, al.GetStationStringWithEquations(station));
                        }

                        else
                        {
                            string s1 = al.GetStationStringWithEquations(station);
                            string s2;

                            bool isNegativeNumber = s1.StartsWith("-");

                            if (isNegativeNumber)
                            {
                                s2 = al.GetStationStringWithEquations(station + 100);
                            }
                            else
                            {
                                s2 = al.GetStationStringWithEquations(station - 100);
                            }

                            double p1 = double.Parse(s1.Remove(s1.IndexOf('+')));
                            double p2 = double.Parse(s2.Remove(s1.IndexOf('+')));

                            if (p1 != p2)
                            {
                                PrintCurrentStationInfo(ed, station, offset, al.GetStationStringWithEquations(station));
                            }
                            else
                            {
                                if (isNegativeNumber)
                                {
                                    foreach (List<double> btwlist in irregularEquations)
                                    {
                                        if (station <= btwlist[0] && station > btwlist[1])
                                        {
                                            double subtracter = 100 - btwlist[2];
                                            string s = al.GetStationStringWithEquations(station);
                                            double digit = double.Parse(s.Substring(s.IndexOf('+')));
                                            digit -= subtracter;
                                            digit += 100;
                                            string stationOffset = s.Remove(s.IndexOf('+')) + "+" + digit;
                                            PrintCurrentStationInfo(ed, station, offset, stationOffset);
                                            //break;
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (List<double> btwlist in irregularEquations)
                                    {
                                        if (station >= btwlist[0] && station < btwlist[1])
                                        {
                                            double subtracter = 100 - btwlist[2];
                                            string s = al.GetStationStringWithEquations(station);
                                            double digit = double.Parse(s.Substring(s.IndexOf('+')));
                                            digit -= subtracter;
                                            digit += 100;
                                            string stationOffset = s.Remove(s.IndexOf('+')) + "+" + digit;
                                            PrintCurrentStationInfo(ed, station, offset, stationOffset);
                                            //break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Autodesk.Civil.CivilException ex)
                    {
                        string msg = ex.Message;
                    }
                    ptRes = doc.Editor.GetPoint(ptOpts);
                }
            }
        }

        private static ObjectId GetAlignment(ref Database db, ref Editor ed)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            db = doc.Database;
            ed = doc.Editor;

            ObjectId alignId = SelectAlignment(ed);
            if (alignId == ObjectId.Null)
            {
                return ObjectId.Null;
            }
            return alignId;
        }

        private static ObjectId SelectAlignment(Editor ed)
        {
            ObjectId result = ObjectId.Null;
            PromptEntityOptions entOpts = new PromptEntityOptions("\nВыберите трассу: ");
            entOpts.SetRejectMessage("...это не трасса, попробуйте снова!:");
            entOpts.AddAllowedClass(typeof(Alignment), true);
            PromptEntityResult entRes = ed.GetEntity(entOpts);
            if (entRes.Status == PromptStatus.OK)
                result = entRes.ObjectId;
            return result;
        }

        private static void GetAlignmentProperties(Database db, ObjectId alignId, ref Alignment al, ref List<List<double>> moreHundredEquations)
        {
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                al = (Alignment)alignId.GetObject(OpenMode.ForRead);

                List<StationEquation> listEquations = al.StationEquations.ToList<StationEquation>();

                moreHundredEquations = new List<List<double>>();

                foreach (StationEquation s in listEquations)
                {
                    if (s.StationBack > s.StationAhead)
                    {
                        List<double> btwlist = new List<double>();
                        double firstValue = s.RawStationBack;
                        double length = Math.Abs(s.StationBack - s.StationAhead);
                        double secondValue = length + Math.Abs(firstValue);

                        if (firstValue < 0)
                        {
                            secondValue *= -1;
                        }

                        btwlist.Add(firstValue);
                        btwlist.Add(secondValue);
                        btwlist.Add(length);

                        moreHundredEquations.Add(btwlist);
                    }
                }
            }
        }

        private static void PrintAtInfo(Editor ed, Alignment al, List<List<double>> irregularEquations)
        {
            ed.WriteMessage("\nНадена трасса - \"{0}\", ее длина = {1}", al.Name, al.Length);

            if (!irregularEquations.Any())
            {
                ed.WriteMessage("\nНа трассе нет неправильных пикетов, больше 100 метров");
            }
            else
            {
                ed.WriteMessage("\nНа трассе имеются неправильные пикеты, больше 100 метров, {0} шт.:", irregularEquations.Count);
                foreach (List<double> btwlist in irregularEquations)
                {
                    ed.WriteMessage("\nНачало - {0}, Конец - {1}, Длина участка - {2} м.", btwlist[0], btwlist[1], btwlist[2] + 100);
                }
            }
        }

        private static void PrintCurrentStationInfo(Editor ed, double station, double offset, string stationOffset)
        {
            ed.WriteMessage("\nПозиция - \"{0}\", смещение выбранной точки: {1}, смещение курсора: {2}", station, stationOffset, offset.ToString("F2"));
        }
    }
}