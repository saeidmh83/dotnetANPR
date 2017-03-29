﻿using System.Collections.Generic;
using System.Linq;
using System.Xml;
using dotNETANPR.Recognizer;

namespace dotNETANPR.Intelligence
{
    public class Parser
    {
        public class PlateForm
        {
            public class Position
            {
                public char[] AllowedChars;

                public Position(string data)
                {
                    AllowedChars = data.ToCharArray();
                }

                public bool IsAllowed(char chr)
                {
                    bool ret = false;
                    foreach (char t in AllowedChars)
                        if (t == chr)
                            ret = true;
                    return ret;
                }
            }

            readonly List<Position> _positions;
            string _name;
            public bool Flagged;

            public PlateForm(string name)
            {
                _name = name;
                _positions = new List<Position>();
            }

            public void AddPosition(Position p)
            {
                _positions.Add(p);
            }

            public Position GetPosition(int index)
            {
                return _positions[index];
            }

            public int Length()
            {
                return _positions.Count;
            }
        }

        public class FinalPlate
        {
            public string Plate { get; set; }
            public float RequiredChanges { get; set; }

            public FinalPlate()
            {
                Plate = string.Empty;
            }

            public void AddChar(char chr)
            {
                Plate = Plate + chr;
            }
        }

        private List<PlateForm> _plateForms;

        public Parser()
        {
            _plateForms = new List<PlateForm>();
            _plateForms = LoadFromXml(
                Intelligence.Configurator.GetPathProperty("intelligence_syntaxDescriptionFile"));
        }

        public List<PlateForm> LoadFromXml(string fileName)
        {
            List<PlateForm> plateForms = new List<PlateForm>();
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            XmlNode structureNode = doc.DocumentElement;
            XmlNodeList structureNodeContent = structureNode.ChildNodes;
            for (int i = 0; i < structureNodeContent.Count; i++)
            {
                XmlNode typeNode = structureNodeContent.Item(i);
                if (!typeNode.Name.Equals("type")) continue;
                PlateForm form = new PlateForm(((XmlElement) typeNode).GetAttribute("name"));
                XmlNodeList typeNodeContent = typeNode.ChildNodes;
                for (int ii = 0; ii < typeNodeContent.Count; ii++)
                {
                    XmlNode charNode = typeNodeContent.Item(ii);
                    if (!charNode.Name.Equals("char")) continue;
                    string content = ((XmlElement) charNode).GetAttribute("content");

                    form.AddPosition(new PlateForm.Position(content.ToUpper()));
                }
                plateForms.Add(form);
            }
            return plateForms;
        }

        public void UnFlagAll()
        {
            foreach (PlateForm form in _plateForms)
                form.Flagged = false;
        }

        public void FlagEqualOrShorterLength(int length)
        {
            bool found = false;
            for (int i = length; i >= 1 && !found; i--)
            {
                foreach (PlateForm form in _plateForms)
                {
                    if (form.Length() == i)
                    {
                        form.Flagged = true;
                        found = true;
                    }
                }
            }
        }

        public void FlagEqualLength(int length)
        {
            foreach (PlateForm form in _plateForms)
            {
                if (form.Length() == length)
                {
                    form.Flagged = true;
                }
            }
        }

        public void InvertFlags()
        {
            foreach (PlateForm form in _plateForms)
                form.Flagged = !form.Flagged;
        }

        public string Parse(RecognizedPlate recognizedPlate, int syntaxAnalysisMode)
        {
            if (syntaxAnalysisMode == 0)
            {
                Program.ReportGenerator.InsertText(" result : " + recognizedPlate.GetString() + " --> <font size=15>" +
                                                   recognizedPlate.GetString() + "</font><hr><br>");
                return recognizedPlate.GetString();
            }

            int length = recognizedPlate.Chars.Count;
            UnFlagAll();
            if (syntaxAnalysisMode == 1)
            {
                FlagEqualLength(length);
            }
            else
            {
                FlagEqualOrShorterLength(length);
            }

            List<FinalPlate> finalPlates = new List<FinalPlate>();

            foreach (PlateForm form in _plateForms)
            {
                if (!form.Flagged) continue;
                for (int i = 0; i <= length - form.Length(); i++)
                {
                    FinalPlate finalPlate = new FinalPlate();
                    for (int ii = 0; ii < form.Length(); ii++)
                    {
                        CharacterRecognizer.RecognizedChar rc = recognizedPlate.GetChar(ii + i);

                        if (form.GetPosition(ii).IsAllowed(rc.GetPattern(0).Character))
                        {
                            finalPlate.AddChar(rc.GetPattern(0).Character);
                        }
                        else
                        {
                            finalPlate.RequiredChanges++;
                            for (int x = 0; x < rc.Patterns.Count; x++)
                            {
                                if (!form.GetPosition(ii).IsAllowed(rc.GetPattern(x).Character)) continue;
                                CharacterRecognizer.RecognizedChar.RecognizedPattern rp = rc.GetPattern(x);
                                finalPlate.RequiredChanges += (rp.Cost / 100);
                                finalPlate.AddChar(rp.Character);
                                break;
                            }
                        }
                    }
                    finalPlates.Add(finalPlate);
                }
            }
            if (finalPlates.Count == 0) return recognizedPlate.GetString();
            float minimalChanges = float.PositiveInfinity;
            int minimalIndex = 0;
            for (int i = 0; i < finalPlates.Count; i++)
            {
                if (!(finalPlates.ElementAt(i).RequiredChanges <= minimalChanges)) continue;
                minimalChanges = finalPlates.ElementAt(i).RequiredChanges;
                minimalIndex = i;
            }

            string toReturn = recognizedPlate.GetString();
            if (finalPlates.ElementAt(minimalIndex).RequiredChanges <= 2)
                toReturn = finalPlates.ElementAt(minimalIndex).Plate;
            return toReturn;
        }
    }
}
