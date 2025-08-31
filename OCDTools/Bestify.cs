using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OCD_Tools
{
    internal class Bestify
    {
        internal static void Bestifying(GH_Document doc, List<GH_Group> groups)
        {
            var record = new GH_UndoRecord("Bestifying");

            foreach (var group in groups)
            {
                record.AddAction(new GH_GenericObjectAction(group));

                var groupBounds = group.Attributes.Bounds;
                var leftBound = groupBounds.Left;
                var rightBound = groupBounds.Right;

                var objects = group.ObjectsRecursive().OfType<IGH_ActiveObject>().ToList();
                var allParams = GetGroupParameters(objects);

                var externalSources = GetExternalSources(allParams);
                var externalTargets = GetExternalTargets(allParams);

                var inputDict = CreateExternalParams(doc, group, record, externalSources, leftBound, true);
                RewireExternalSources(group, record, externalSources, inputDict, leftBound);

                var outputDict = CreateExternalParams(doc, group, record, externalTargets, rightBound, false);
                RewireExternalTargets(group, record, externalTargets, outputDict, rightBound);

                group.ExpireCaches();
                group.ExpirePreview(true);

            }

            RefreshCanvas(doc, record);
        }

        private static List<IGH_Param> GetGroupParameters(List<IGH_ActiveObject> objects)
        {
            var paramList = objects.OfType<IGH_Param>().ToList();

            foreach (var component in objects.OfType<IGH_Component>())
            {
                paramList.AddRange(component.Params.Input);
                paramList.AddRange(component.Params.Output);
            }

            return paramList;
        }

        private static Dictionary<IGH_Param, List<IGH_Param>> GetExternalSources(List<IGH_Param> parameters)
        {
            var externalSources = new Dictionary<IGH_Param, List<IGH_Param>>();

            foreach (var param in parameters)
            {
                var sources = param.Sources.Where(s => !parameters.Contains(s)).ToList();
                if (sources.Any()) externalSources[param] = sources;
            }

            return externalSources;
        }

        private static Dictionary<IGH_Param, List<IGH_Param>> GetExternalTargets(List<IGH_Param> parameters)
        {
            var externalTargets = new Dictionary<IGH_Param, List<IGH_Param>>();

            foreach (var param in parameters)
            {
                var targets = param.Recipients.Where(t => !parameters.Contains(t)).ToList();
                if (targets.Any()) externalTargets[param] = targets;
            }

            return externalTargets;
        }

        private static Dictionary<Guid, IGH_Param> CreateExternalParams(GH_Document doc, GH_Group group, GH_UndoRecord record,
            Dictionary<IGH_Param, List<IGH_Param>> externalParams, float bound, bool isInput)
        {
            var paramDict = new Dictionary<Guid, IGH_Param>();
            var guidRecord = new List<Guid>();
            List<IGH_Param> val = new List<IGH_Param>();

            foreach (var kvp in externalParams)
            {
                var externalPs = kvp.Value;
                if (kvp.Key.ComponentGuid.ToString().Contains("8ec86459-bf01-4409-baee-174d0d2b13d0") && kvp.Key.Attributes.HasInputGrip && kvp.Key.Attributes.HasOutputGrip)
                {
                    if (isInput)
                    {
                        foreach (var externalParam in externalPs)
                        {
                            guidRecord.Add(externalParam.InstanceGuid);
                        }
                    }
                    else
                    {
                        if (kvp.Key.Sources.Count > 0)
                        {
                            guidRecord.Add(kvp.Key.Sources.First().InstanceGuid);
                        }
                    }
                }
                else
                {
                    foreach (var v in externalPs)
                    {
                        if (guidRecord.Contains(v.InstanceGuid) && isInput)
                        {
                            paramDict[v.InstanceGuid] = kvp.Key;
                        }
                        else if (guidRecord.Contains(kvp.Key.InstanceGuid))
                        {
                            paramDict[v.InstanceGuid] = kvp.Key;
                        }
                        else
                        {
                            if (isInput)
                            {
                                val.Add(v);
                            }
                            else
                            {
                                val.Add(kvp.Key);
                            }
                        }
                    }
                }

            }
            val = val.Distinct().ToList();

            foreach (var externalParam in val)
            {

                var genericParam = new Param_GenericObject
                {
                    NickName = externalParam.NickName,
                    Hidden = true,
                    IconDisplayMode = GH_IconDisplayMode.name
                };

                doc.AddObject(genericParam, false);
                record.AddAction(new GH_AddObjectAction(genericParam));

                if (isInput)
                    genericParam.AddSource(externalParam);

                genericParam.Attributes.ExpireLayout();
                genericParam.Attributes.PerformLayout();
                genericParam.ExpireSolution(false);

                paramDict[externalParam.InstanceGuid] = genericParam;
                group.AddObject(genericParam.InstanceGuid);

            }

            return paramDict;
        }

        private static void RewireExternalSources(GH_Group group, GH_UndoRecord record,
            Dictionary<IGH_Param, List<IGH_Param>> externalSources, Dictionary<Guid, IGH_Param> inputDict, float leftBound)
        {
            var visited = new List<Guid>();
            foreach (var kvp in externalSources)
            {
                var param = kvp.Key;
                var condition = false;
                record.AddAction(new GH_WireAction(param));
                if (!((param.ComponentGuid.ToString().Contains("8ec86459-bf01-4409-baee-174d0d2b13d0") && kvp.Key.Attributes.HasInputGrip && kvp.Key.Attributes.HasOutputGrip) ||
                    visited.Contains(param.InstanceGuid)))
                {
                    param.RemoveAllSources();
                }
                else condition = true;


                foreach (var sourceParam in kvp.Value)
                {

                    IGH_Param genericParam;
                    if (!inputDict.TryGetValue(sourceParam.InstanceGuid, out genericParam))
                    {
                        continue;
                    }
                    if (!condition)
                    {
                        param.AddSource(genericParam);
                        genericParam.Attributes.Pivot = new PointF(
    leftBound - genericParam.Attributes.Bounds.Width,
    param.Attributes.Pivot.Y);
                        genericParam.NickName = param.NickName;
                    }
                    else
                    {
                        if (!visited.Contains(genericParam.InstanceGuid))
                        {
                            record.AddAction(new GH_WireAction(genericParam));
                            visited.Add(genericParam.InstanceGuid);
                            genericParam.RemoveSource(param.Sources[0]);
                            genericParam.AddSource(param);
                        }
                    }
                }

                group.ExpireCaches();
            }
        }

        private static void RewireExternalTargets(GH_Group group, GH_UndoRecord record,
            Dictionary<IGH_Param, List<IGH_Param>> externalTargets, Dictionary<Guid, IGH_Param> outputDict, float rightBound)
        {
            foreach (var kvp in externalTargets)
            {
                var param = kvp.Key;
                IGH_Param genericParam;
                if (!outputDict.TryGetValue(param.InstanceGuid, out genericParam))
                {
                    if (kvp.Key.ComponentGuid.ToString().Contains("8ec86459-bf01-4409-baee-174d0d2b13d0") && kvp.Key.Attributes.HasInputGrip && kvp.Key.Attributes.HasOutputGrip)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (var targetParam in kvp.Value)
                        {
                            if (outputDict.TryGetValue(targetParam.InstanceGuid, out genericParam))
                            {
                                foreach (var gen in genericParam.Recipients)
                                {
                                    if (gen.ComponentGuid.ToString().Contains("8ec86459-bf01-4409-baee-174d0d2b13d0") && gen.Attributes.HasInputGrip && gen.Attributes.HasOutputGrip)
                                    {
                                        record.AddAction(new GH_WireAction(targetParam));
                                        targetParam.RemoveSource(param.InstanceGuid);
                                        targetParam.AddSource(gen);
                                        gen.ExpirePreview(true);
                                        targetParam.ExpirePreview(true);
                                        group.ExpireCaches();
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    genericParam.AddSource(param);

                    genericParam.Attributes.Pivot = new PointF(
                        rightBound + genericParam.Attributes.Bounds.Width,
                        param.Attributes.Pivot.Y);
                    genericParam.NickName = param.NickName;

                    foreach (var targetParam in kvp.Value)
                    {
                        record.AddAction(new GH_WireAction(targetParam));
                        targetParam.RemoveSource(param.InstanceGuid);
                        targetParam.AddSource(genericParam);
                    }
                }

                group.ExpireCaches();
            }
        }

        private static void RefreshCanvas(GH_Document doc, GH_UndoRecord record)
        {
            Instances.ActiveCanvas.Refresh();
            Instances.RedrawAll();
            Instances.InvalidateCanvas();
            doc.UndoUtil.RecordEvent(record);
        }
    }
}