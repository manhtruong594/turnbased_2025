#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TurnBasedGame.ObjectPool;
using TurnBasedGame.VFX;
using UCP.Bridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class VfxParticleController : IUCPScript
{
    public string Name => "vfx-controller";
    public string Description => "Inspect, modify, manage hierarchy, scale, recolor, validate and render multi-frame previews of Unity ParticleSystem (VFX) prefabs and scene objects.";

    public object Execute(string paramsJson)
    {
        var p = string.IsNullOrEmpty(paramsJson)
            ? new Dictionary<string, object>()
            : MiniJson.Deserialize(paramsJson) as Dictionary<string, object>;

        if (p == null)
            return Error("Invalid JSON parameters");

        string action = GetString(p, "action", "inspect").ToLowerInvariant();

        try
        {
            return action switch
            {
                "inspect" => HandleInspect(p),
                "modify" => HandleModify(p),
                "manage-hierarchy" => HandleManageHierarchy(p),
                "scale-vfx" => HandleScaleVfx(p),
                "recolor-vfx" => HandleRecolorVfx(p),
                "preview" => HandlePreview(p),
                "validate" => HandleValidate(p),
                _ => Error($"Unknown action '{action}'. Supported actions: inspect, modify, manage-hierarchy, scale-vfx, recolor-vfx, preview, validate.")
            };
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["action"] = action,
                ["error"] = ex.Message,
                ["stackTrace"] = ex.StackTrace
            };
        }
    }

    #region 1. INSPECT

    private object HandleInspect(Dictionary<string, object> p)
    {
        string prefabPath = GetString(p, "prefabPath", GetString(p, "path", null));
        long instanceId = GetLong(p, "instanceId", 0);
        string filterNode = GetString(p, "nodePath", null);

        GameObject targetRoot = ResolveReadOnlyTarget(prefabPath, instanceId, out bool isPrefabAsset, out string resolvedPath);
        if (targetRoot == null)
            return Error("Target not found. Provide valid 'prefabPath' or 'instanceId'.");

        var emitters = new List<object>();
        var systems = targetRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in systems)
        {
            string relPath = GetRelativePath(targetRoot.transform, ps.transform);
            if (!string.IsNullOrEmpty(filterNode) && !PathMatches(relPath, filterNode, ps.name))
                continue;

            emitters.Add(SerializeParticleSystem(ps, relPath));
        }

        var trails = new List<object>();
        foreach (var tr in targetRoot.GetComponentsInChildren<TrailRenderer>(true))
        {
            trails.Add(new Dictionary<string, object>
            {
                ["nodePath"] = GetRelativePath(targetRoot.transform, tr.transform),
                ["enabled"] = tr.enabled,
                ["time"] = (double)tr.time,
                ["startWidth"] = (double)tr.startWidth,
                ["endWidth"] = (double)tr.endWidth,
                ["minVertexDistance"] = (double)tr.minVertexDistance,
                ["emitting"] = tr.emitting,
                ["material"] = tr.sharedMaterial != null ? AssetDatabase.GetAssetPath(tr.sharedMaterial) : null,
                ["shader"] = tr.sharedMaterial != null && tr.sharedMaterial.shader != null ? tr.sharedMaterial.shader.name : null
            });
        }

        var lines = new List<object>();
        foreach (var lr in targetRoot.GetComponentsInChildren<LineRenderer>(true))
        {
            lines.Add(new Dictionary<string, object>
            {
                ["nodePath"] = GetRelativePath(targetRoot.transform, lr.transform),
                ["enabled"] = lr.enabled,
                ["positionCount"] = lr.positionCount,
                ["startWidth"] = (double)lr.startWidth,
                ["endWidth"] = (double)lr.endWidth,
                ["useWorldSpace"] = lr.useWorldSpace,
                ["loop"] = lr.loop,
                ["material"] = lr.sharedMaterial != null ? AssetDatabase.GetAssetPath(lr.sharedMaterial) : null,
                ["shader"] = lr.sharedMaterial != null && lr.sharedMaterial.shader != null ? lr.sharedMaterial.shader.name : null
            });
        }

        var lights = new List<object>();
        foreach (var lt in targetRoot.GetComponentsInChildren<Light>(true))
        {
            lights.Add(new Dictionary<string, object>
            {
                ["nodePath"] = GetRelativePath(targetRoot.transform, lt.transform),
                ["enabled"] = lt.enabled,
                ["type"] = lt.type.ToString(),
                ["color"] = ColorToHex(lt.color),
                ["intensity"] = (double)lt.intensity,
                ["range"] = (double)lt.range
            });
        }

        return new Dictionary<string, object>
        {
            ["success"] = true,
            ["action"] = "inspect",
            ["name"] = targetRoot.name,
            ["isPrefabAsset"] = isPrefabAsset,
            ["prefabPath"] = resolvedPath,
            ["instanceId"] = targetRoot.GetInstanceID(),
            ["gameplayComponents"] = InspectGameplayComponents(targetRoot),
            ["emitterCount"] = emitters.Count,
            ["emitters"] = emitters,
            ["trailRenderers"] = trails,
            ["lineRenderers"] = lines,
            ["lights"] = lights
        };
    }

    private Dictionary<string, object> InspectGameplayComponents(GameObject root)
    {
        var hasPooledObject = root.GetComponent<PooledObject>() != null;
        var hasPooledVfx = root.GetComponent<PooledVFX>() != null;
        var projectile = root.GetComponent<Projectile>();
        var laser = root.GetComponent<LaserProjectile>();
        var buff = root.GetComponent<BuffEffect>();

        var dict = new Dictionary<string, object>
        {
            ["hasPooledObject"] = hasPooledObject,
            ["hasPooledVFX"] = hasPooledVfx,
            ["hasProjectile"] = projectile != null,
            ["hasLaserProjectile"] = laser != null,
            ["hasBuffEffect"] = buff != null
        };

        if (buff != null)
            dict["buffEffectType"] = buff.effectType.ToString();

        return dict;
    }

    private Dictionary<string, object> SerializeParticleSystem(ParticleSystem ps, string nodePath)
    {
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var vol = ps.velocityOverLifetime;
        var lvol = ps.limitVelocityOverLifetime;
        var fol = ps.forceOverLifetime;
        var col = ps.colorOverLifetime;
        var sol = ps.sizeOverLifetime;
        var rol = ps.rotationOverLifetime;
        var noise = ps.noise;
        var tsa = ps.textureSheetAnimation;
        var trails = ps.trails;
        var psr = ps.GetComponent<ParticleSystemRenderer>();

        var burstsList = new List<object>();
        int burstCount = emission.burstCount;
        if (burstCount > 0)
        {
            var bursts = new ParticleSystem.Burst[burstCount];
            emission.GetBursts(bursts);
            for (int i = 0; i < burstCount; i++)
            {
                var b = bursts[i];
                burstsList.Add(new Dictionary<string, object>
                {
                    ["time"] = (double)b.time,
                    ["count"] = SerializeMinMaxCurve(b.count),
                    ["minCount"] = (int)b.minCount,
                    ["maxCount"] = (int)b.maxCount,
                    ["cycleCount"] = b.cycleCount,
                    ["repeatInterval"] = (double)b.repeatInterval,
                    ["probability"] = (double)b.probability
                });
            }
        }

        var result = new Dictionary<string, object>
        {
            ["nodePath"] = nodePath,
            ["name"] = ps.name,
            ["activeSelf"] = ps.gameObject.activeSelf,
            ["localPosition"] = Vec3ToList(ps.transform.localPosition),
            ["localEulerAngles"] = Vec3ToList(ps.transform.localEulerAngles),
            ["localScale"] = Vec3ToList(ps.transform.localScale),
            ["main"] = new Dictionary<string, object>
            {
                ["duration"] = (double)main.duration,
                ["loop"] = main.loop,
                ["prewarm"] = main.prewarm,
                ["startDelay"] = SerializeMinMaxCurve(main.startDelay),
                ["startLifetime"] = SerializeMinMaxCurve(main.startLifetime),
                ["startSpeed"] = SerializeMinMaxCurve(main.startSpeed),
                ["startSize3D"] = main.startSize3D,
                ["startSize"] = SerializeMinMaxCurve(main.startSize),
                ["startSizeX"] = SerializeMinMaxCurve(main.startSizeX),
                ["startSizeY"] = SerializeMinMaxCurve(main.startSizeY),
                ["startSizeZ"] = SerializeMinMaxCurve(main.startSizeZ),
                ["startRotation3D"] = main.startRotation3D,
                ["startRotation"] = SerializeMinMaxCurve(main.startRotation, true),
                ["startRotationX"] = SerializeMinMaxCurve(main.startRotationX, true),
                ["startRotationY"] = SerializeMinMaxCurve(main.startRotationY, true),
                ["startRotationZ"] = SerializeMinMaxCurve(main.startRotationZ, true),
                ["flipRotation"] = (double)main.flipRotation,
                ["startColor"] = SerializeMinMaxGradient(main.startColor),
                ["gravityModifier"] = SerializeMinMaxCurve(main.gravityModifier),
                ["simulationSpace"] = main.simulationSpace.ToString(),
                ["simulationSpeed"] = (double)main.simulationSpeed,
                ["scalingMode"] = main.scalingMode.ToString(),
                ["playOnAwake"] = main.playOnAwake,
                ["maxParticles"] = main.maxParticles,
                ["stopAction"] = main.stopAction.ToString(),
                ["cullingMode"] = main.cullingMode.ToString()
            },
            ["emission"] = new Dictionary<string, object>
            {
                ["enabled"] = emission.enabled,
                ["rateOverTime"] = SerializeMinMaxCurve(emission.rateOverTime),
                ["rateOverDistance"] = SerializeMinMaxCurve(emission.rateOverDistance),
                ["burstCount"] = burstCount,
                ["bursts"] = burstsList
            },
            ["shape"] = new Dictionary<string, object>
            {
                ["enabled"] = shape.enabled,
                ["shapeType"] = shape.shapeType.ToString(),
                ["angle"] = (double)shape.angle,
                ["radius"] = (double)shape.radius,
                ["radiusThickness"] = (double)shape.radiusThickness,
                ["arc"] = (double)shape.arc,
                ["arcMode"] = shape.arcMode.ToString(),
                ["arcSpread"] = (double)shape.arcSpread,
                ["donutRadius"] = (double)shape.donutRadius,
                ["position"] = Vec3ToList(shape.position),
                ["rotation"] = Vec3ToList(shape.rotation),
                ["scale"] = Vec3ToList(shape.scale),
                ["alignToDirection"] = shape.alignToDirection,
                ["randomDirectionAmount"] = (double)shape.randomDirectionAmount,
                ["sphericalDirectionAmount"] = (double)shape.sphericalDirectionAmount,
                ["randomPositionAmount"] = (double)shape.randomPositionAmount
            },
            ["velocityOverLifetime"] = new Dictionary<string, object>
            {
                ["enabled"] = vol.enabled,
                ["space"] = vol.space.ToString(),
                ["x"] = SerializeMinMaxCurve(vol.x),
                ["y"] = SerializeMinMaxCurve(vol.y),
                ["z"] = SerializeMinMaxCurve(vol.z),
                ["orbitalX"] = SerializeMinMaxCurve(vol.orbitalX),
                ["orbitalY"] = SerializeMinMaxCurve(vol.orbitalY),
                ["orbitalZ"] = SerializeMinMaxCurve(vol.orbitalZ),
                ["radial"] = SerializeMinMaxCurve(vol.radial),
                ["speedModifier"] = SerializeMinMaxCurve(vol.speedModifier)
            },
            ["limitVelocityOverLifetime"] = new Dictionary<string, object>
            {
                ["enabled"] = lvol.enabled,
                ["separateAxes"] = lvol.separateAxes,
                ["space"] = lvol.space.ToString(),
                ["limit"] = SerializeMinMaxCurve(lvol.limit),
                ["dampen"] = (double)lvol.dampen,
                ["drag"] = SerializeMinMaxCurve(lvol.drag),
                ["multiplyDragByParticleSize"] = lvol.multiplyDragByParticleSize,
                ["multiplyDragByParticleVelocity"] = lvol.multiplyDragByParticleVelocity
            },
            ["forceOverLifetime"] = new Dictionary<string, object>
            {
                ["enabled"] = fol.enabled,
                ["space"] = fol.space.ToString(),
                ["randomized"] = fol.randomized,
                ["x"] = SerializeMinMaxCurve(fol.x),
                ["y"] = SerializeMinMaxCurve(fol.y),
                ["z"] = SerializeMinMaxCurve(fol.z)
            },
            ["colorOverLifetime"] = new Dictionary<string, object>
            {
                ["enabled"] = col.enabled,
                ["color"] = SerializeMinMaxGradient(col.color)
            },
            ["sizeOverLifetime"] = new Dictionary<string, object>
            {
                ["enabled"] = sol.enabled,
                ["separateAxes"] = sol.separateAxes,
                ["size"] = SerializeMinMaxCurve(sol.size),
                ["x"] = SerializeMinMaxCurve(sol.x),
                ["y"] = SerializeMinMaxCurve(sol.y),
                ["z"] = SerializeMinMaxCurve(sol.z)
            },
            ["rotationOverLifetime"] = new Dictionary<string, object>
            {
                ["enabled"] = rol.enabled,
                ["separateAxes"] = rol.separateAxes,
                ["x"] = SerializeMinMaxCurve(rol.x, true),
                ["y"] = SerializeMinMaxCurve(rol.y, true),
                ["z"] = SerializeMinMaxCurve(rol.z, true)
            },
            ["noise"] = new Dictionary<string, object>
            {
                ["enabled"] = noise.enabled,
                ["separateAxes"] = noise.separateAxes,
                ["strength"] = SerializeMinMaxCurve(noise.strength),
                ["frequency"] = (double)noise.frequency,
                ["scrollSpeed"] = SerializeMinMaxCurve(noise.scrollSpeed),
                ["damping"] = noise.damping,
                ["octaveCount"] = noise.octaveCount,
                ["octaveMultiplier"] = (double)noise.octaveMultiplier,
                ["octaveScale"] = (double)noise.octaveScale,
                ["quality"] = noise.quality.ToString(),
                ["positionAmount"] = SerializeMinMaxCurve(noise.positionAmount),
                ["rotationAmount"] = SerializeMinMaxCurve(noise.rotationAmount),
                ["sizeAmount"] = SerializeMinMaxCurve(noise.sizeAmount)
            },
            ["textureSheetAnimation"] = new Dictionary<string, object>
            {
                ["enabled"] = tsa.enabled,
                ["mode"] = tsa.mode.ToString(),
                ["numTilesX"] = tsa.numTilesX,
                ["numTilesY"] = tsa.numTilesY,
                ["animation"] = tsa.animation.ToString(),
                ["frameOverTime"] = SerializeMinMaxCurve(tsa.frameOverTime),
                ["startFrame"] = SerializeMinMaxCurve(tsa.startFrame),
                ["cycleCount"] = tsa.cycleCount
            },
            ["trails"] = new Dictionary<string, object>
            {
                ["enabled"] = trails.enabled,
                ["mode"] = trails.mode.ToString(),
                ["ratio"] = (double)trails.ratio,
                ["lifetime"] = SerializeMinMaxCurve(trails.lifetime),
                ["minVertexDistance"] = (double)trails.minVertexDistance,
                ["worldSpace"] = trails.worldSpace,
                ["dieWithParticles"] = trails.dieWithParticles,
                ["textureMode"] = trails.textureMode.ToString(),
                ["sizeAffectsWidth"] = trails.sizeAffectsWidth,
                ["sizeAffectsLifetime"] = trails.sizeAffectsLifetime,
                ["inheritParticleColor"] = trails.inheritParticleColor,
                ["colorOverLifetime"] = SerializeMinMaxGradient(trails.colorOverLifetime),
                ["widthOverTrail"] = SerializeMinMaxCurve(trails.widthOverTrail),
                ["colorOverTrail"] = SerializeMinMaxGradient(trails.colorOverTrail)
            }
        };

        if (psr != null)
        {
            result["renderer"] = new Dictionary<string, object>
            {
                ["enabled"] = psr.enabled,
                ["renderMode"] = psr.renderMode.ToString(),
                ["alignment"] = psr.alignment.ToString(),
                ["normalDirection"] = (double)psr.normalDirection,
                ["cameraVelocityScale"] = (double)psr.cameraVelocityScale,
                ["velocityScale"] = (double)psr.velocityScale,
                ["lengthScale"] = (double)psr.lengthScale,
                ["sortingFudge"] = (double)psr.sortingFudge,
                ["minParticleSize"] = (double)psr.minParticleSize,
                ["maxParticleSize"] = (double)psr.maxParticleSize,
                ["pivot"] = Vec3ToList(psr.pivot),
                ["flip"] = Vec3ToList(psr.flip),
                ["sortMode"] = psr.sortMode.ToString(),
                ["sortingLayerName"] = psr.sortingLayerName,
                ["sortingOrder"] = psr.sortingOrder,
                ["shadowCastingMode"] = psr.shadowCastingMode.ToString(),
                ["receiveShadows"] = psr.receiveShadows,
                ["sharedMaterial"] = psr.sharedMaterial != null ? AssetDatabase.GetAssetPath(psr.sharedMaterial) : null,
                ["sharedMaterialName"] = psr.sharedMaterial != null ? psr.sharedMaterial.name : null,
                ["shader"] = psr.sharedMaterial != null && psr.sharedMaterial.shader != null ? psr.sharedMaterial.shader.name : null,
                ["trailMaterial"] = psr.trailMaterial != null ? AssetDatabase.GetAssetPath(psr.trailMaterial) : null,
                ["mesh"] = psr.mesh != null ? AssetDatabase.GetAssetPath(psr.mesh) : null,
                ["meshName"] = psr.mesh != null ? psr.mesh.name : null
            };
        }

        return result;
    }

    #endregion

    #region 2. MODIFY

    private object HandleModify(Dictionary<string, object> p)
    {
        string prefabPath = GetString(p, "prefabPath", GetString(p, "path", null));
        long instanceId = GetLong(p, "instanceId", 0);

        var logs = new List<string>();

        return ExecuteOnTarget(prefabPath, instanceId, (root, isPrefab) =>
        {
            // Support either an "emitters" array or single emitter fields directly on root param object
            var emitterList = new List<Dictionary<string, object>>();
            if (p.TryGetValue("emitters", out var emObj) && emObj is IList emArr)
            {
                foreach (var item in emArr)
                {
                    if (item is Dictionary<string, object> d)
                        emitterList.Add(d);
                }
            }
            else
            {
                emitterList.Add(p);
            }

            int modifiedCount = 0;
            foreach (var emSpec in emitterList)
            {
                string nodePath = GetString(emSpec, "nodePath", ".");
                ParticleSystem ps = FindParticleSystemByNodePath(root, nodePath);
                if (ps == null)
                {
                    logs.Add($"WARNING: ParticleSystem not found at nodePath '{nodePath}'");
                    continue;
                }

                if (!isPrefab)
                {
                    Undo.RecordObject(ps, "UCP Modify ParticleSystem");
                    Undo.RecordObject(ps.transform, "UCP Modify ParticleSystem Transform");
                    var psr = ps.GetComponent<ParticleSystemRenderer>();
                    if (psr != null) Undo.RecordObject(psr, "UCP Modify ParticleSystemRenderer");
                }

                ApplyEmitterModifications(ps, emSpec, logs);
                EditorUtility.SetDirty(ps);
                modifiedCount++;
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["action"] = "modify",
                ["target"] = root.name,
                ["prefabPath"] = prefabPath,
                ["modifiedEmitterCount"] = modifiedCount,
                ["logs"] = logs
            };
        });
    }

    private void ApplyEmitterModifications(ParticleSystem ps, Dictionary<string, object> spec, List<string> logs)
    {
        string label = ps.name;

        if (spec.TryGetValue("activeSelf", out var activeObj))
            ps.gameObject.SetActive(Convert.ToBoolean(activeObj));
        if (spec.TryGetValue("localPosition", out var posObj))
            ps.transform.localPosition = ParseVec3(posObj, ps.transform.localPosition);
        if (spec.TryGetValue("localEulerAngles", out var rotObj))
            ps.transform.localEulerAngles = ParseVec3(rotObj, ps.transform.localEulerAngles);
        if (spec.TryGetValue("localScale", out var scaleObj))
            ps.transform.localScale = ParseVec3(scaleObj, ps.transform.localScale);

        if (spec.TryGetValue("main", out var mainObj) && mainObj is Dictionary<string, object> m)
        {
            var main = ps.main;
            if (m.TryGetValue("duration", out var v)) main.duration = Convert.ToSingle(v);
            if (m.TryGetValue("loop", out v)) main.loop = Convert.ToBoolean(v);
            if (m.TryGetValue("prewarm", out v)) main.prewarm = Convert.ToBoolean(v);
            if (m.TryGetValue("startDelay", out v)) main.startDelay = ParseMinMaxCurve(v, main.startDelay);
            if (m.TryGetValue("startLifetime", out v)) main.startLifetime = ParseMinMaxCurve(v, main.startLifetime);
            if (m.TryGetValue("startSpeed", out v)) main.startSpeed = ParseMinMaxCurve(v, main.startSpeed);
            if (m.TryGetValue("startSize3D", out v)) main.startSize3D = Convert.ToBoolean(v);
            if (m.TryGetValue("startSize", out v)) main.startSize = ParseMinMaxCurve(v, main.startSize);
            if (m.TryGetValue("startSizeX", out v)) main.startSizeX = ParseMinMaxCurve(v, main.startSizeX);
            if (m.TryGetValue("startSizeY", out v)) main.startSizeY = ParseMinMaxCurve(v, main.startSizeY);
            if (m.TryGetValue("startSizeZ", out v)) main.startSizeZ = ParseMinMaxCurve(v, main.startSizeZ);
            if (m.TryGetValue("startRotation3D", out v)) main.startRotation3D = Convert.ToBoolean(v);
            if (m.TryGetValue("startRotation", out v)) main.startRotation = ParseMinMaxCurve(v, main.startRotation, true);
            if (m.TryGetValue("startRotationX", out v)) main.startRotationX = ParseMinMaxCurve(v, main.startRotationX, true);
            if (m.TryGetValue("startRotationY", out v)) main.startRotationY = ParseMinMaxCurve(v, main.startRotationY, true);
            if (m.TryGetValue("startRotationZ", out v)) main.startRotationZ = ParseMinMaxCurve(v, main.startRotationZ, true);
            if (m.TryGetValue("flipRotation", out v)) main.flipRotation = Convert.ToSingle(v);
            if (m.TryGetValue("startColor", out v)) main.startColor = ParseMinMaxGradient(v, main.startColor);
            if (m.TryGetValue("gravityModifier", out v)) main.gravityModifier = ParseMinMaxCurve(v, main.gravityModifier);
            if (m.TryGetValue("simulationSpace", out v) && TryParseEnum<ParticleSystemSimulationSpace>(v, out var simSpace))
                main.simulationSpace = simSpace;
            if (m.TryGetValue("simulationSpeed", out v)) main.simulationSpeed = Convert.ToSingle(v);
            if (m.TryGetValue("scalingMode", out v) && TryParseEnum<ParticleSystemScalingMode>(v, out var scaleMode))
                main.scalingMode = scaleMode;
            if (m.TryGetValue("playOnAwake", out v)) main.playOnAwake = Convert.ToBoolean(v);
            if (m.TryGetValue("maxParticles", out v)) main.maxParticles = Convert.ToInt32(v);
            if (m.TryGetValue("stopAction", out v) && TryParseEnum<ParticleSystemStopAction>(v, out var stopAct))
                main.stopAction = stopAct;
            logs.Add($"[{label}] Updated MainModule");
        }

        if (spec.TryGetValue("emission", out var emObj) && emObj is Dictionary<string, object> em)
        {
            var emission = ps.emission;
            if (em.TryGetValue("enabled", out var v)) emission.enabled = Convert.ToBoolean(v);
            if (em.TryGetValue("rateOverTime", out v)) emission.rateOverTime = ParseMinMaxCurve(v, emission.rateOverTime);
            if (em.TryGetValue("rateOverDistance", out v)) emission.rateOverDistance = ParseMinMaxCurve(v, emission.rateOverDistance);
            if (em.TryGetValue("bursts", out v) && v is IList bList)
            {
                var bursts = new ParticleSystem.Burst[bList.Count];
                for (int i = 0; i < bList.Count; i++)
                {
                    if (bList[i] is Dictionary<string, object> bd)
                    {
                        float time = GetFloat(bd, "time", 0f);
                        int cycles = GetInt(bd, "cycleCount", 1);
                        float interval = GetFloat(bd, "repeatInterval", 0.01f);
                        float prob = GetFloat(bd, "probability", 1f);

                        ParticleSystem.Burst burst;
                        if (bd.TryGetValue("count", out var cntObj))
                        {
                            var countCurve = ParseMinMaxCurve(cntObj, new ParticleSystem.MinMaxCurve(10f));
                            burst = new ParticleSystem.Burst(time, countCurve, cycles, interval);
                        }
                        else if (bd.ContainsKey("minCount") || bd.ContainsKey("maxCount"))
                        {
                            short minC = (short)GetInt(bd, "minCount", 10);
                            short maxC = (short)GetInt(bd, "maxCount", minC);
                            burst = new ParticleSystem.Burst(time, minC, maxC, cycles, interval);
                        }
                        else
                        {
                            burst = new ParticleSystem.Burst(time, 10);
                        }
                        burst.probability = prob;
                        bursts[i] = burst;
                    }
                }
                emission.SetBursts(bursts);
            }
            logs.Add($"[{label}] Updated EmissionModule");
        }

        if (spec.TryGetValue("shape", out var shObj) && shObj is Dictionary<string, object> sh)
        {
            var shape = ps.shape;
            if (sh.TryGetValue("enabled", out var v)) shape.enabled = Convert.ToBoolean(v);
            if (sh.TryGetValue("shapeType", out v) && TryParseEnum<ParticleSystemShapeType>(v, out var st))
                shape.shapeType = st;
            if (sh.TryGetValue("angle", out v)) shape.angle = Convert.ToSingle(v);
            if (sh.TryGetValue("radius", out v)) shape.radius = Convert.ToSingle(v);
            if (sh.TryGetValue("radiusThickness", out v)) shape.radiusThickness = Convert.ToSingle(v);
            if (sh.TryGetValue("arc", out v)) shape.arc = Convert.ToSingle(v);
            if (sh.TryGetValue("arcMode", out v) && TryParseEnum<ParticleSystemShapeMultiModeValue>(v, out var am))
                shape.arcMode = am;
            if (sh.TryGetValue("arcSpread", out v)) shape.arcSpread = Convert.ToSingle(v);
            if (sh.TryGetValue("donutRadius", out v)) shape.donutRadius = Convert.ToSingle(v);
            if (sh.TryGetValue("position", out v)) shape.position = ParseVec3(v, shape.position);
            if (sh.TryGetValue("rotation", out v)) shape.rotation = ParseVec3(v, shape.rotation);
            if (sh.TryGetValue("scale", out v)) shape.scale = ParseVec3(v, shape.scale);
            if (sh.TryGetValue("alignToDirection", out v)) shape.alignToDirection = Convert.ToBoolean(v);
            if (sh.TryGetValue("randomDirectionAmount", out v)) shape.randomDirectionAmount = Convert.ToSingle(v);
            if (sh.TryGetValue("sphericalDirectionAmount", out v)) shape.sphericalDirectionAmount = Convert.ToSingle(v);
            if (sh.TryGetValue("randomPositionAmount", out v)) shape.randomPositionAmount = Convert.ToSingle(v);
            logs.Add($"[{label}] Updated ShapeModule");
        }

        if (spec.TryGetValue("velocityOverLifetime", out var volObj) && volObj is Dictionary<string, object> vd)
        {
            var vol = ps.velocityOverLifetime;
            if (vd.TryGetValue("enabled", out var v)) vol.enabled = Convert.ToBoolean(v);
            if (vd.TryGetValue("space", out v) && TryParseEnum<ParticleSystemSimulationSpace>(v, out var sp))
                vol.space = sp;
            if (vd.TryGetValue("x", out v)) vol.x = ParseMinMaxCurve(v, vol.x);
            if (vd.TryGetValue("y", out v)) vol.y = ParseMinMaxCurve(v, vol.y);
            if (vd.TryGetValue("z", out v)) vol.z = ParseMinMaxCurve(v, vol.z);
            if (vd.TryGetValue("orbitalX", out v)) vol.orbitalX = ParseMinMaxCurve(v, vol.orbitalX);
            if (vd.TryGetValue("orbitalY", out v)) vol.orbitalY = ParseMinMaxCurve(v, vol.orbitalY);
            if (vd.TryGetValue("orbitalZ", out v)) vol.orbitalZ = ParseMinMaxCurve(v, vol.orbitalZ);
            if (vd.TryGetValue("radial", out v)) vol.radial = ParseMinMaxCurve(v, vol.radial);
            if (vd.TryGetValue("speedModifier", out v)) vol.speedModifier = ParseMinMaxCurve(v, vol.speedModifier);
            logs.Add($"[{label}] Updated VelocityOverLifetimeModule");
        }

        if (spec.TryGetValue("limitVelocityOverLifetime", out var lvolObj) && lvolObj is Dictionary<string, object> lvd)
        {
            var lvol = ps.limitVelocityOverLifetime;
            if (lvd.TryGetValue("enabled", out var v)) lvol.enabled = Convert.ToBoolean(v);
            if (lvd.TryGetValue("separateAxes", out v)) lvol.separateAxes = Convert.ToBoolean(v);
            if (lvd.TryGetValue("space", out v) && TryParseEnum<ParticleSystemSimulationSpace>(v, out var sp))
                lvol.space = sp;
            if (lvd.TryGetValue("limit", out v)) lvol.limit = ParseMinMaxCurve(v, lvol.limit);
            if (lvd.TryGetValue("limitX", out v)) lvol.limitX = ParseMinMaxCurve(v, lvol.limitX);
            if (lvd.TryGetValue("limitY", out v)) lvol.limitY = ParseMinMaxCurve(v, lvol.limitY);
            if (lvd.TryGetValue("limitZ", out v)) lvol.limitZ = ParseMinMaxCurve(v, lvol.limitZ);
            if (lvd.TryGetValue("dampen", out v)) lvol.dampen = Convert.ToSingle(v);
            if (lvd.TryGetValue("drag", out v)) lvol.drag = ParseMinMaxCurve(v, lvol.drag);
            if (lvd.TryGetValue("multiplyDragByParticleSize", out v)) lvol.multiplyDragByParticleSize = Convert.ToBoolean(v);
            if (lvd.TryGetValue("multiplyDragByParticleVelocity", out v)) lvol.multiplyDragByParticleVelocity = Convert.ToBoolean(v);
            logs.Add($"[{label}] Updated LimitVelocityOverLifetimeModule");
        }

        if (spec.TryGetValue("forceOverLifetime", out var folObj) && folObj is Dictionary<string, object> fd)
        {
            var fol = ps.forceOverLifetime;
            if (fd.TryGetValue("enabled", out var v)) fol.enabled = Convert.ToBoolean(v);
            if (fd.TryGetValue("space", out v) && TryParseEnum<ParticleSystemSimulationSpace>(v, out var sp))
                fol.space = sp;
            if (fd.TryGetValue("randomized", out v)) fol.randomized = Convert.ToBoolean(v);
            if (fd.TryGetValue("x", out v)) fol.x = ParseMinMaxCurve(v, fol.x);
            if (fd.TryGetValue("y", out v)) fol.y = ParseMinMaxCurve(v, fol.y);
            if (fd.TryGetValue("z", out v)) fol.z = ParseMinMaxCurve(v, fol.z);
            logs.Add($"[{label}] Updated ForceOverLifetimeModule");
        }

        if (spec.TryGetValue("colorOverLifetime", out var colObj) && colObj is Dictionary<string, object> cd)
        {
            var col = ps.colorOverLifetime;
            if (cd.TryGetValue("enabled", out var v)) col.enabled = Convert.ToBoolean(v);
            if (cd.TryGetValue("color", out v)) col.color = ParseMinMaxGradient(v, col.color);
            logs.Add($"[{label}] Updated ColorOverLifetimeModule");
        }

        if (spec.TryGetValue("sizeOverLifetime", out var solObj) && solObj is Dictionary<string, object> sd)
        {
            var sol = ps.sizeOverLifetime;
            if (sd.TryGetValue("enabled", out var v)) sol.enabled = Convert.ToBoolean(v);
            if (sd.TryGetValue("separateAxes", out v)) sol.separateAxes = Convert.ToBoolean(v);
            if (sd.TryGetValue("size", out v)) sol.size = ParseMinMaxCurve(v, sol.size);
            if (sd.TryGetValue("x", out v)) sol.x = ParseMinMaxCurve(v, sol.x);
            if (sd.TryGetValue("y", out v)) sol.y = ParseMinMaxCurve(v, sol.y);
            if (sd.TryGetValue("z", out v)) sol.z = ParseMinMaxCurve(v, sol.z);
            logs.Add($"[{label}] Updated SizeOverLifetimeModule");
        }

        if (spec.TryGetValue("rotationOverLifetime", out var rolObj) && rolObj is Dictionary<string, object> rd)
        {
            var rol = ps.rotationOverLifetime;
            if (rd.TryGetValue("enabled", out var v)) rol.enabled = Convert.ToBoolean(v);
            if (rd.TryGetValue("separateAxes", out v)) rol.separateAxes = Convert.ToBoolean(v);
            if (rd.TryGetValue("x", out v)) rol.x = ParseMinMaxCurve(v, rol.x, true);
            if (rd.TryGetValue("y", out v)) rol.y = ParseMinMaxCurve(v, rol.y, true);
            if (rd.TryGetValue("z", out v)) rol.z = ParseMinMaxCurve(v, rol.z, true);
            logs.Add($"[{label}] Updated RotationOverLifetimeModule");
        }

        if (spec.TryGetValue("noise", out var nsObj) && nsObj is Dictionary<string, object> nd)
        {
            var noise = ps.noise;
            if (nd.TryGetValue("enabled", out var v)) noise.enabled = Convert.ToBoolean(v);
            if (nd.TryGetValue("separateAxes", out v)) noise.separateAxes = Convert.ToBoolean(v);
            if (nd.TryGetValue("strength", out v)) noise.strength = ParseMinMaxCurve(v, noise.strength);
            if (nd.TryGetValue("frequency", out v)) noise.frequency = Convert.ToSingle(v);
            if (nd.TryGetValue("scrollSpeed", out v)) noise.scrollSpeed = ParseMinMaxCurve(v, noise.scrollSpeed);
            if (nd.TryGetValue("damping", out v)) noise.damping = Convert.ToBoolean(v);
            if (nd.TryGetValue("octaveCount", out v)) noise.octaveCount = Convert.ToInt32(v);
            if (nd.TryGetValue("octaveMultiplier", out v)) noise.octaveMultiplier = Convert.ToSingle(v);
            if (nd.TryGetValue("octaveScale", out v)) noise.octaveScale = Convert.ToSingle(v);
            if (nd.TryGetValue("quality", out v) && TryParseEnum<ParticleSystemNoiseQuality>(v, out var nq))
                noise.quality = nq;
            if (nd.TryGetValue("positionAmount", out v)) noise.positionAmount = ParseMinMaxCurve(v, noise.positionAmount);
            if (nd.TryGetValue("rotationAmount", out v)) noise.rotationAmount = ParseMinMaxCurve(v, noise.rotationAmount);
            if (nd.TryGetValue("sizeAmount", out v)) noise.sizeAmount = ParseMinMaxCurve(v, noise.sizeAmount);
            logs.Add($"[{label}] Updated NoiseModule");
        }

        if (spec.TryGetValue("textureSheetAnimation", out var tsaObj) && tsaObj is Dictionary<string, object> td)
        {
            var tsa = ps.textureSheetAnimation;
            if (td.TryGetValue("enabled", out var v)) tsa.enabled = Convert.ToBoolean(v);
            if (td.TryGetValue("mode", out v) && TryParseEnum<ParticleSystemAnimationMode>(v, out var am))
                tsa.mode = am;
            if (td.TryGetValue("numTilesX", out v)) tsa.numTilesX = Convert.ToInt32(v);
            if (td.TryGetValue("numTilesY", out v)) tsa.numTilesY = Convert.ToInt32(v);
            if (td.TryGetValue("animation", out v) && TryParseEnum<ParticleSystemAnimationType>(v, out var at))
                tsa.animation = at;
            if (td.TryGetValue("frameOverTime", out v)) tsa.frameOverTime = ParseMinMaxCurve(v, tsa.frameOverTime);
            if (td.TryGetValue("startFrame", out v)) tsa.startFrame = ParseMinMaxCurve(v, tsa.startFrame);
            if (td.TryGetValue("cycleCount", out v)) tsa.cycleCount = Convert.ToInt32(v);
            logs.Add($"[{label}] Updated TextureSheetAnimationModule");
        }

        if (spec.TryGetValue("trails", out var trObj) && trObj is Dictionary<string, object> trd)
        {
            var trails = ps.trails;
            if (trd.TryGetValue("enabled", out var v)) trails.enabled = Convert.ToBoolean(v);
            if (trd.TryGetValue("mode", out v) && TryParseEnum<ParticleSystemTrailMode>(v, out var tm))
                trails.mode = tm;
            if (trd.TryGetValue("ratio", out v)) trails.ratio = Convert.ToSingle(v);
            if (trd.TryGetValue("lifetime", out v)) trails.lifetime = ParseMinMaxCurve(v, trails.lifetime);
            if (trd.TryGetValue("minVertexDistance", out v)) trails.minVertexDistance = Convert.ToSingle(v);
            if (trd.TryGetValue("worldSpace", out v)) trails.worldSpace = Convert.ToBoolean(v);
            if (trd.TryGetValue("dieWithParticles", out v)) trails.dieWithParticles = Convert.ToBoolean(v);
            if (trd.TryGetValue("textureMode", out v) && TryParseEnum<ParticleSystemTrailTextureMode>(v, out var ttm))
                trails.textureMode = ttm;
            if (trd.TryGetValue("sizeAffectsWidth", out v)) trails.sizeAffectsWidth = Convert.ToBoolean(v);
            if (trd.TryGetValue("sizeAffectsLifetime", out v)) trails.sizeAffectsLifetime = Convert.ToBoolean(v);
            if (trd.TryGetValue("inheritParticleColor", out v)) trails.inheritParticleColor = Convert.ToBoolean(v);
            if (trd.TryGetValue("colorOverLifetime", out v)) trails.colorOverLifetime = ParseMinMaxGradient(v, trails.colorOverLifetime);
            if (trd.TryGetValue("widthOverTrail", out v)) trails.widthOverTrail = ParseMinMaxCurve(v, trails.widthOverTrail);
            if (trd.TryGetValue("colorOverTrail", out v)) trails.colorOverTrail = ParseMinMaxGradient(v, trails.colorOverTrail);
            logs.Add($"[{label}] Updated TrailModule");
        }

        if (spec.TryGetValue("renderer", out var renObj) && renObj is Dictionary<string, object> rnd)
        {
            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                if (rnd.TryGetValue("enabled", out var v)) psr.enabled = Convert.ToBoolean(v);
                if (rnd.TryGetValue("renderMode", out v) && TryParseEnum<ParticleSystemRenderMode>(v, out var rm))
                    psr.renderMode = rm;
                if (rnd.TryGetValue("alignment", out v) && TryParseEnum<ParticleSystemRenderSpace>(v, out var al))
                    psr.alignment = al;
                if (rnd.TryGetValue("normalDirection", out v)) psr.normalDirection = Convert.ToSingle(v);
                if (rnd.TryGetValue("cameraVelocityScale", out v)) psr.cameraVelocityScale = Convert.ToSingle(v);
                if (rnd.TryGetValue("velocityScale", out v)) psr.velocityScale = Convert.ToSingle(v);
                if (rnd.TryGetValue("lengthScale", out v)) psr.lengthScale = Convert.ToSingle(v);
                if (rnd.TryGetValue("sortingFudge", out v)) psr.sortingFudge = Convert.ToSingle(v);
                if (rnd.TryGetValue("minParticleSize", out v)) psr.minParticleSize = Convert.ToSingle(v);
                if (rnd.TryGetValue("maxParticleSize", out v)) psr.maxParticleSize = Convert.ToSingle(v);
                if (rnd.TryGetValue("pivot", out v)) psr.pivot = ParseVec3(v, psr.pivot);
                if (rnd.TryGetValue("flip", out v)) psr.flip = ParseVec3(v, psr.flip);
                if (rnd.TryGetValue("sortMode", out v) && TryParseEnum<ParticleSystemSortMode>(v, out var sm))
                    psr.sortMode = sm;
                if (rnd.TryGetValue("sortingLayerName", out v)) psr.sortingLayerName = v?.ToString() ?? "Default";
                if (rnd.TryGetValue("sortingOrder", out v)) psr.sortingOrder = Convert.ToInt32(v);
                if (rnd.TryGetValue("shadowCastingMode", out v) && TryParseEnum<ShadowCastingMode>(v, out var scm))
                    psr.shadowCastingMode = scm;
                if (rnd.TryGetValue("receiveShadows", out v)) psr.receiveShadows = Convert.ToBoolean(v);
                if (rnd.TryGetValue("sharedMaterial", out v) && v != null)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(v.ToString());
                    if (mat != null) psr.sharedMaterial = mat;
                    else logs.Add($"WARNING: Material not found at '{v}'");
                }
                if (rnd.TryGetValue("trailMaterial", out v) && v != null)
                {
                    var tMat = AssetDatabase.LoadAssetAtPath<Material>(v.ToString());
                    if (tMat != null) psr.trailMaterial = tMat;
                    else logs.Add($"WARNING: Trail Material not found at '{v}'");
                }
                if (rnd.TryGetValue("mesh", out v) && v != null)
                {
                    var mesh = ResolveMesh(v.ToString());
                    if (mesh != null) psr.mesh = mesh;
                    else logs.Add($"WARNING: Mesh not found at '{v}'");
                }
                EditorUtility.SetDirty(psr);
                logs.Add($"[{label}] Updated ParticleSystemRenderer");
            }
        }
    }

    #endregion

    #region 3. MANAGE HIERARCHY

    private object HandleManageHierarchy(Dictionary<string, object> p)
    {
        string prefabPath = GetString(p, "prefabPath", GetString(p, "path", null));
        long instanceId = GetLong(p, "instanceId", 0);
        string operation = GetString(p, "operation", "add-emitter").ToLowerInvariant();
        string nodePath = GetString(p, "nodePath", ".");
        string newName = GetString(p, "name", "NewEmitter");
        string parentPath = GetString(p, "parentPath", ".");

        var logs = new List<string>();

        return ExecuteOnTarget(prefabPath, instanceId, (root, isPrefab) =>
        {
            switch (operation)
            {
                case "add-emitter":
                {
                    Transform parent = FindTransformByNodePath(root.transform, parentPath) ?? root.transform;
                    var go = new GameObject(newName);
                    if (!isPrefab) Undo.RegisterCreatedObjectUndo(go, "UCP Add Particle Emitter");
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    var ps = go.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.loop = false;
                    main.playOnAwake = true;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;

                    if (p.TryGetValue("config", out var cfgObj) && cfgObj is Dictionary<string, object> cfg)
                        ApplyEmitterModifications(ps, cfg, logs);

                    logs.Add($"Created child emitter '{GetRelativePath(root.transform, go.transform)}'");
                    break;
                }
                case "duplicate-emitter":
                {
                    ParticleSystem source = FindParticleSystemByNodePath(root, nodePath);
                    if (source == null)
                        return Error($"Source emitter '{nodePath}' not found");
                    if (source.gameObject == root)
                        return Error("Cannot duplicate root object itself; specify a child emitter or add a child.");

                    var clone = Object.Instantiate(source.gameObject, source.transform.parent);
                    clone.name = string.IsNullOrEmpty(newName) ? source.name + "_Copy" : newName;
                    clone.transform.localPosition = source.transform.localPosition;
                    clone.transform.localRotation = source.transform.localRotation;
                    clone.transform.localScale = source.transform.localScale;
                    if (!isPrefab) Undo.RegisterCreatedObjectUndo(clone, "UCP Duplicate Emitter");

                    if (p.TryGetValue("config", out var cfgObj) && cfgObj is Dictionary<string, object> cfg)
                    {
                        var clonePs = clone.GetComponent<ParticleSystem>();
                        if (clonePs != null) ApplyEmitterModifications(clonePs, cfg, logs);
                    }

                    logs.Add($"Duplicated '{nodePath}' -> '{GetRelativePath(root.transform, clone.transform)}'");
                    break;
                }
                case "remove-emitter":
                {
                    Transform target = FindTransformByNodePath(root.transform, nodePath);
                    if (target == null || target == root.transform)
                        return Error($"Cannot remove emitter at '{nodePath}' (not found or is root)");

                    string removedPath = GetRelativePath(root.transform, target);
                    if (!isPrefab)
                        Undo.DestroyObjectImmediate(target.gameObject);
                    else
                        Object.DestroyImmediate(target.gameObject);

                    logs.Add($"Removed emitter '{removedPath}'");
                    break;
                }
                case "rename-emitter":
                {
                    Transform target = FindTransformByNodePath(root.transform, nodePath);
                    if (target == null)
                        return Error($"Emitter '{nodePath}' not found");
                    if (!isPrefab) Undo.RecordObject(target.gameObject, "UCP Rename Emitter");
                    target.gameObject.name = newName;
                    logs.Add($"Renamed '{nodePath}' -> '{newName}'");
                    break;
                }
                default:
                    return Error($"Unknown hierarchy operation '{operation}'. Supported: add-emitter, duplicate-emitter, remove-emitter, rename-emitter.");
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["action"] = "manage-hierarchy",
                ["operation"] = operation,
                ["logs"] = logs
            };
        });
    }

    #endregion

    #region 4. SCALE VFX

    private object HandleScaleVfx(Dictionary<string, object> p)
    {
        string prefabPath = GetString(p, "prefabPath", GetString(p, "path", null));
        long instanceId = GetLong(p, "instanceId", 0);
        float factor = GetFloat(p, "scaleFactor", 1f);
        bool scaleSpeed = GetBool(p, "scaleSpeed", true);
        bool scaleShape = GetBool(p, "scaleShape", true);
        bool scaleGravity = GetBool(p, "scaleGravity", false);

        if (factor <= 0.001f)
            return Error("scaleFactor must be > 0.001");

        var logs = new List<string>();

        return ExecuteOnTarget(prefabPath, instanceId, (root, isPrefab) =>
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (!isPrefab) Undo.RecordObject(ps, "UCP Scale VFX");

                var main = ps.main;
                main.startSize = MultiplyMinMaxCurve(main.startSize, factor);
                if (main.startSize3D)
                {
                    main.startSizeX = MultiplyMinMaxCurve(main.startSizeX, factor);
                    main.startSizeY = MultiplyMinMaxCurve(main.startSizeY, factor);
                    main.startSizeZ = MultiplyMinMaxCurve(main.startSizeZ, factor);
                }

                if (scaleSpeed)
                    main.startSpeed = MultiplyMinMaxCurve(main.startSpeed, factor);

                if (scaleGravity)
                    main.gravityModifier = MultiplyMinMaxCurve(main.gravityModifier, factor);

                if (scaleShape)
                {
                    var shape = ps.shape;
                    if (shape.enabled)
                    {
                        shape.radius *= factor;
                        shape.donutRadius *= factor;
                        shape.scale *= factor;
                        shape.position *= factor;
                    }
                }

                if (ps.transform != root.transform)
                {
                    if (!isPrefab) Undo.RecordObject(ps.transform, "UCP Scale VFX Transform");
                    ps.transform.localPosition *= factor;
                }

                EditorUtility.SetDirty(ps);
                logs.Add($"Scaled '{GetRelativePath(root.transform, ps.transform)}' by {factor:F2}x");
            }

            foreach (var tr in root.GetComponentsInChildren<TrailRenderer>(true))
            {
                if (!isPrefab) Undo.RecordObject(tr, "UCP Scale Trail");
                tr.startWidth *= factor;
                tr.endWidth *= factor;
                EditorUtility.SetDirty(tr);
            }

            foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
            {
                if (!isPrefab) Undo.RecordObject(lr, "UCP Scale Line");
                lr.startWidth *= factor;
                lr.endWidth *= factor;
                EditorUtility.SetDirty(lr);
            }

            foreach (var lt in root.GetComponentsInChildren<Light>(true))
            {
                if (!isPrefab) Undo.RecordObject(lt, "UCP Scale Light");
                lt.range *= factor;
                EditorUtility.SetDirty(lt);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["action"] = "scale-vfx",
                ["scaleFactor"] = (double)factor,
                ["scaledEmitterCount"] = systems.Length,
                ["logs"] = logs
            };
        });
    }

    #endregion

    #region 5. RECOLOR VFX

    private object HandleRecolorVfx(Dictionary<string, object> p)
    {
        string prefabPath = GetString(p, "prefabPath", GetString(p, "path", null));
        long instanceId = GetLong(p, "instanceId", 0);
        string primaryHex = GetString(p, "primaryColor", null);
        string secondaryHex = GetString(p, "secondaryColor", null);
        float hueShift = GetFloat(p, "hueShift", 0f); // -180 to +180 degrees
        float saturationMult = GetFloat(p, "saturationMultiplier", 1f);
        float valueMult = GetFloat(p, "valueMultiplier", 1f);

        bool useTargetPalette = !string.IsNullOrEmpty(primaryHex);
        Color primaryColor = useTargetPalette ? ParseColor(primaryHex, Color.white) : Color.white;
        Color secondaryColor = !string.IsNullOrEmpty(secondaryHex)
            ? ParseColor(secondaryHex, primaryColor * 0.6f)
            : Color.Lerp(primaryColor, Color.black, 0.35f);

        var logs = new List<string>();

        return ExecuteOnTarget(prefabPath, instanceId, (root, isPrefab) =>
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (!isPrefab) Undo.RecordObject(ps, "UCP Recolor VFX");

                var main = ps.main;
                main.startColor = RecolorMinMaxGradient(main.startColor, useTargetPalette, primaryColor, secondaryColor, hueShift, saturationMult, valueMult);

                var col = ps.colorOverLifetime;
                if (col.enabled)
                    col.color = RecolorMinMaxGradient(col.color, useTargetPalette, primaryColor, secondaryColor, hueShift, saturationMult, valueMult);

                var trails = ps.trails;
                if (trails.enabled)
                {
                    trails.colorOverLifetime = RecolorMinMaxGradient(trails.colorOverLifetime, useTargetPalette, primaryColor, secondaryColor, hueShift, saturationMult, valueMult);
                    trails.colorOverTrail = RecolorMinMaxGradient(trails.colorOverTrail, useTargetPalette, primaryColor, secondaryColor, hueShift, saturationMult, valueMult);
                }

                EditorUtility.SetDirty(ps);
                logs.Add($"Recolored emitter '{GetRelativePath(root.transform, ps.transform)}'");
            }

            foreach (var lt in root.GetComponentsInChildren<Light>(true))
            {
                if (!isPrefab) Undo.RecordObject(lt, "UCP Recolor Light");
                lt.color = TransformColor(lt.color, useTargetPalette, primaryColor, secondaryColor, 0f, hueShift, saturationMult, valueMult);
                EditorUtility.SetDirty(lt);
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["action"] = "recolor-vfx",
                ["updatedEmitters"] = systems.Length,
                ["logs"] = logs
            };
        });
    }

    #endregion

    #region 6. PREVIEW & MULTI-FRAME SIMULATION

    private object HandlePreview(Dictionary<string, object> p)
    {
        string prefabPath = GetString(p, "prefabPath", GetString(p, "path", null));
        long instanceId = GetLong(p, "instanceId", 0);
        int width = Mathf.Clamp(GetInt(p, "width", 1024), 256, 2048);
        int height = Mathf.Clamp(GetInt(p, "height", 1024), 256, 2048);
        float cameraDistance = GetFloat(p, "cameraDistance", 4.5f);
        float cameraElevation = GetFloat(p, "cameraElevation", 25f);
        float cameraAzimuth = GetFloat(p, "cameraAzimuth", 35f);
        string bgColorHex = GetString(p, "backgroundColor", "#14161D");
        string outputPrefix = GetString(p, "outputPrefix", null);

        GameObject sourceObj = ResolveReadOnlyTarget(prefabPath, instanceId, out bool isPrefabAsset, out string resolvedPath);
        if (sourceObj == null)
            return Error("Target not found for preview. Provide 'prefabPath' or 'instanceId'.");

        Vector3 stageOrigin = new Vector3(500f, 500f, 500f);
        GameObject tempInstance = isPrefabAsset
            ? (GameObject)PrefabUtility.InstantiatePrefab(sourceObj)
            : Object.Instantiate(sourceObj);

        GameObject camObj = null;
        GameObject lightObj = null;

        try
        {
            tempInstance.name = "__UCP_VFX_Preview_Temp";
            tempInstance.transform.position = stageOrigin;
            tempInstance.transform.rotation = Quaternion.identity;
            tempInstance.SetActive(true);

            var systems = tempInstance.GetComponentsInChildren<ParticleSystem>(true);
            if (systems.Length == 0)
                return Error($"No ParticleSystem found on '{sourceObj.name}'");

            // Determine sample times
            float maxEffectDuration = EstimateTotalEffectDuration(systems);
            var sampleTimes = new List<float>();
            if (p.TryGetValue("sampleTimes", out var stObj) && stObj is IList stList && stList.Count > 0)
            {
                foreach (var item in stList)
                    sampleTimes.Add(Mathf.Max(0.001f, Convert.ToSingle(item)));
            }
            else
            {
                sampleTimes.Add(Mathf.Max(0.03f, maxEffectDuration * 0.08f));
                sampleTimes.Add(Mathf.Max(0.12f, maxEffectDuration * 0.28f));
                sampleTimes.Add(Mathf.Max(0.25f, maxEffectDuration * 0.55f));
                sampleTimes.Add(Mathf.Max(0.45f, maxEffectDuration * 0.85f));
            }

            Directory.CreateDirectory("Artifacts");
            string safeName = string.IsNullOrEmpty(outputPrefix)
                ? SanitizeFileName(sourceObj.name)
                : SanitizeFileName(outputPrefix);

            camObj = new GameObject("__UCP_VFX_Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ParseColor(bgColorHex, new Color(0.08f, 0.09f, 0.11f, 1f));
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 45f;

            Quaternion camRot = Quaternion.Euler(cameraElevation, cameraAzimuth + 180f, 0f);
            Vector3 focusCenter = stageOrigin + Vector3.up * GetFloat(p, "focusHeightOffset", 0.5f);
            camObj.transform.position = focusCenter - (camRot * Vector3.forward) * cameraDistance;
            camObj.transform.LookAt(focusCenter);

            lightObj = new GameObject("__UCP_VFX_Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            ParticleSystem rootPs = tempInstance.GetComponent<ParticleSystem>() ?? systems[0];
            var frames = new List<object>();

            for (int i = 0; i < sampleTimes.Count; i++)
            {
                float t = sampleTimes[i];

                // Reset and simulate from 0 -> t using fixed steps so Velocity/Noise/Trails evaluate accurately
                rootPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                rootPs.Clear(true);
                SimulateDeterministically(rootPs, t);

                int totalParticles = 0;
                var emitterCounts = new Dictionary<string, object>();
                foreach (var ps in systems)
                {
                    if (!ps.gameObject.activeInHierarchy) continue;
                    int count = ps.particleCount;
                    totalParticles += count;
                    emitterCounts[GetRelativePath(tempInstance.transform, ps.transform)] = count;
                }

                string filePath = $"Artifacts/vfx_{safeName}_f{i + 1}_{t:F2}s.png";
                CaptureCameraToPng(cam, filePath, width, height);

                frames.Add(new Dictionary<string, object>
                {
                    ["frameIndex"] = i + 1,
                    ["time"] = (double)t,
                    ["totalParticleCount"] = totalParticles,
                    ["emitterParticleCounts"] = emitterCounts,
                    ["imagePath"] = filePath,
                    ["absoluteImagePath"] = Path.GetFullPath(filePath).Replace("\\", "/")
                });
            }

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["action"] = "preview",
                ["name"] = sourceObj.name,
                ["estimatedDuration"] = (double)maxEffectDuration,
                ["cameraDistance"] = (double)cameraDistance,
                ["frames"] = frames
            };
        }
        finally
        {
            if (camObj != null) Object.DestroyImmediate(camObj);
            if (lightObj != null) Object.DestroyImmediate(lightObj);
            if (tempInstance != null) Object.DestroyImmediate(tempInstance);
        }
    }

    private static void SimulateDeterministically(ParticleSystem rootPs, float targetTime)
    {
        const float step = 1f / 60f;
        rootPs.Simulate(0f, true, true, true);
        float elapsed = 0f;
        while (elapsed + step < targetTime)
        {
            rootPs.Simulate(step, true, false, true);
            elapsed += step;
        }
        float remainder = targetTime - elapsed;
        if (remainder > 0.0001f)
            rootPs.Simulate(remainder, true, false, true);
    }

    private static float EstimateTotalEffectDuration(ParticleSystem[] systems)
    {
        float maxTime = 0.5f;
        foreach (var ps in systems)
        {
            if (ps == null || !ps.gameObject.activeSelf) continue;
            var m = ps.main;
            float delay = GetCurveMax(m.startDelay);
            float life = GetCurveMax(m.startLifetime);
            float sysEnd = delay + m.duration + life;
            if (sysEnd > maxTime) maxTime = sysEnd;
        }
        return Mathf.Clamp(maxTime, 0.2f, 10f);
    }

    #endregion

    #region 7. VALIDATE

    private object HandleValidate(Dictionary<string, object> p)
    {
        string prefabPath = GetString(p, "prefabPath", GetString(p, "path", null));
        long instanceId = GetLong(p, "instanceId", 0);

        GameObject root = ResolveReadOnlyTarget(prefabPath, instanceId, out bool isPrefabAsset, out string resolvedPath);
        if (root == null)
            return Error("Target not found for validation.");

        var errors = new List<string>();
        var warnings = new List<string>();
        var systems = root.GetComponentsInChildren<ParticleSystem>(true);

        if (systems.Length == 0 && root.GetComponentInChildren<LineRenderer>(true) == null && root.GetComponentInChildren<TrailRenderer>(true) == null)
            errors.Add("Prefab contains no ParticleSystem, TrailRenderer, or LineRenderer.");

        bool hasFiniteEmitter = false;
        bool hasLoopEmitter = false;
        int totalMaxParticles = 0;

        foreach (var ps in systems)
        {
            string relPath = GetRelativePath(root.transform, ps.transform);
            var main = ps.main;
            totalMaxParticles += main.maxParticles;

            if (main.loop) hasLoopEmitter = true;
            else hasFiniteEmitter = true;

            if (main.stopAction == ParticleSystemStopAction.Destroy)
            {
                warnings.Add($"[{relPath}] stopAction is 'Destroy'. For pooled VFX (ObjectPoolManager), this will destroy pooled instances instead of returning them to the pool. Set stopAction to 'None' or 'Disable'.");
            }

            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null && psr.enabled)
            {
                if (psr.sharedMaterial == null)
                {
                    errors.Add($"[{relPath}] ParticleSystemRenderer is missing sharedMaterial (will render pink).");
                }
                else if (psr.sharedMaterial.shader == null || psr.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
                {
                    errors.Add($"[{relPath}] Material '{psr.sharedMaterial.name}' has broken/error shader.");
                }

                if (psr.renderMode == ParticleSystemRenderMode.Mesh && psr.mesh == null)
                {
                    errors.Add($"[{relPath}] RenderMode is 'Mesh' but mesh reference is null.");
                }

                if (ps.trails.enabled && psr.trailMaterial == null)
                {
                    warnings.Add($"[{relPath}] Trails module is enabled but trailMaterial is null on ParticleSystemRenderer.");
                }
            }
        }

        bool isProjectile = root.GetComponent<Projectile>() != null;
        bool isBuff = root.GetComponent<BuffEffect>() != null;
        bool hasPooledVfx = root.GetComponent<PooledVFX>() != null;
        bool hasPooledObj = root.GetComponent<PooledObject>() != null;

        if (!isProjectile && !isBuff && hasLoopEmitter && !hasFiniteEmitter)
        {
            warnings.Add("All ParticleSystems on this non-projectile/non-buff VFX have 'loop = true'. SkillEffectRunner will force despawn after 3.0s fallback, or PooledVFX will never finish naturally.");
        }

        if (!isProjectile && !hasPooledObj)
        {
            warnings.Add("Root does not have 'PooledObject' / 'PooledVFX' attached. ObjectPoolManager / SkillEffectRunner will add runtime components dynamically, but pre-attaching them on the prefab is cleaner.");
        }

        if (totalMaxParticles > 3000)
        {
            warnings.Add($"Total maxParticles across all emitters is high ({totalMaxParticles}). Consider tightening maxParticles to realistic bounds.");
        }

        return new Dictionary<string, object>
        {
            ["success"] = true,
            ["action"] = "validate",
            ["valid"] = errors.Count == 0,
            ["name"] = root.name,
            ["prefabPath"] = resolvedPath,
            ["emitterCount"] = systems.Length,
            ["totalMaxParticles"] = totalMaxParticles,
            ["errors"] = errors,
            ["warnings"] = warnings
        };
    }

    #endregion

    #region Serialization & Parsing Helpers (MinMaxCurve & MinMaxGradient)

    private static Dictionary<string, object> SerializeMinMaxCurve(ParticleSystem.MinMaxCurve curve, bool radiansToDegrees = false)
    {
        float factor = radiansToDegrees ? Mathf.Rad2Deg : 1f;
        var dict = new Dictionary<string, object>
        {
            ["mode"] = curve.mode.ToString()
        };

        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                dict["constant"] = (double)(curve.constant * factor);
                break;
            case ParticleSystemCurveMode.TwoConstants:
                dict["min"] = (double)(curve.constantMin * factor);
                dict["max"] = (double)(curve.constantMax * factor);
                break;
            case ParticleSystemCurveMode.Curve:
                dict["multiplier"] = (double)(curve.curveMultiplier * factor);
                dict["keys"] = SerializeAnimationCurve(curve.curve);
                break;
            case ParticleSystemCurveMode.TwoCurves:
                dict["multiplier"] = (double)(curve.curveMultiplier * factor);
                dict["keysMin"] = SerializeAnimationCurve(curve.curveMin);
                dict["keysMax"] = SerializeAnimationCurve(curve.curveMax);
                break;
        }
        return dict;
    }

    private static List<object> SerializeAnimationCurve(AnimationCurve ac)
    {
        var list = new List<object>();
        if (ac == null || ac.keys == null) return list;
        foreach (var k in ac.keys)
        {
            list.Add(new Dictionary<string, object>
            {
                ["time"] = (double)k.time,
                ["value"] = (double)k.value,
                ["inTangent"] = (double)k.inTangent,
                ["outTangent"] = (double)k.outTangent
            });
        }
        return list;
    }

    private static ParticleSystem.MinMaxCurve ParseMinMaxCurve(object val, ParticleSystem.MinMaxCurve fallback, bool degreesToRadians = false)
    {
        float factor = degreesToRadians ? Mathf.Deg2Rad : 1f;

        if (val is double || val is float || val is long || val is int)
        {
            return new ParticleSystem.MinMaxCurve(Convert.ToSingle(val) * factor);
        }

        if (val is IList arr && arr.Count >= 2)
        {
            return new ParticleSystem.MinMaxCurve(
                Convert.ToSingle(arr[0]) * factor,
                Convert.ToSingle(arr[1]) * factor);
        }

        if (val is Dictionary<string, object> d)
        {
            if (d.TryGetValue("preset", out var presetObj) && presetObj != null)
            {
                float mult = GetFloat(d, "multiplier", GetFloat(d, "constant", 1f)) * factor;
                return new ParticleSystem.MinMaxCurve(mult, BuildPresetCurve(presetObj.ToString()));
            }

            string modeStr = GetString(d, "mode", null);
            if (string.IsNullOrEmpty(modeStr))
            {
                if (d.ContainsKey("keys")) modeStr = "Curve";
                else if (d.ContainsKey("keysMin") || d.ContainsKey("keysMax")) modeStr = "TwoCurves";
                else if (d.ContainsKey("min") || d.ContainsKey("max")) modeStr = "TwoConstants";
                else modeStr = "Constant";
            }

            switch (modeStr)
            {
                case "Constant":
                    return new ParticleSystem.MinMaxCurve(GetFloat(d, "constant", 0f) * factor);
                case "TwoConstants":
                    return new ParticleSystem.MinMaxCurve(
                        GetFloat(d, "min", 0f) * factor,
                        GetFloat(d, "max", 1f) * factor);
                case "Curve":
                {
                    float mult = GetFloat(d, "multiplier", 1f) * factor;
                    AnimationCurve ac = d.TryGetValue("keys", out var kObj) ? ParseAnimationCurve(kObj) : AnimationCurve.Linear(0f, 1f, 1f, 1f);
                    return new ParticleSystem.MinMaxCurve(mult, ac);
                }
                case "TwoCurves":
                {
                    float mult = GetFloat(d, "multiplier", 1f) * factor;
                    AnimationCurve acMin = d.TryGetValue("keysMin", out var kmObj) ? ParseAnimationCurve(kmObj) : AnimationCurve.Linear(0f, 0f, 1f, 0f);
                    AnimationCurve acMax = d.TryGetValue("keysMax", out var kxObj) ? ParseAnimationCurve(kxObj) : AnimationCurve.Linear(0f, 1f, 1f, 1f);
                    return new ParticleSystem.MinMaxCurve(mult, acMin, acMax);
                }
            }
        }

        return fallback;
    }

    private static AnimationCurve ParseAnimationCurve(object keysObj)
    {
        if (keysObj is string presetName)
            return BuildPresetCurve(presetName);

        var ac = new AnimationCurve();
        if (keysObj is IList list)
        {
            var keyframes = new List<Keyframe>();
            foreach (var item in list)
            {
                if (item is Dictionary<string, object> kd)
                {
                    float t = GetFloat(kd, "time", 0f);
                    float v = GetFloat(kd, "value", 1f);
                    if (kd.ContainsKey("inTangent") || kd.ContainsKey("outTangent"))
                    {
                        float inT = GetFloat(kd, "inTangent", 0f);
                        float outT = GetFloat(kd, "outTangent", 0f);
                        keyframes.Add(new Keyframe(t, v, inT, outT));
                    }
                    else
                    {
                        keyframes.Add(new Keyframe(t, v));
                    }
                }
            }
            ac.keys = keyframes.ToArray();
        }
        return ac;
    }

    private static AnimationCurve BuildPresetCurve(string preset)
    {
        return (preset ?? "").ToLowerInvariant() switch
        {
            "linearup" => AnimationCurve.Linear(0f, 0f, 1f, 1f),
            "lineardown" => AnimationCurve.Linear(0f, 1f, 1f, 0f),
            "easeout" => new AnimationCurve(
                new Keyframe(0f, 1f, 0f, -0.2f),
                new Keyframe(0.7f, 0.4f, -1.2f, -1.2f),
                new Keyframe(1f, 0f, -1.5f, 0f)),
            "easein" => new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2f),
                new Keyframe(0.3f, 0.8f, 1f, 1f),
                new Keyframe(1f, 1f, 0f, 0f)),
            "popandshrink" => new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 8f),
                new Keyframe(0.15f, 1f, 0f, -0.5f),
                new Keyframe(0.75f, 0.5f, -1.2f, -1.2f),
                new Keyframe(1f, 0f, -2f, 0f)),
            "bell" => new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 3f),
                new Keyframe(0.5f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -3f, 0f)),
            "spike" => new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 12f),
                new Keyframe(0.08f, 1f, 0f, -4f),
                new Keyframe(0.4f, 0.15f, -1f, -0.5f),
                new Keyframe(1f, 0f, -0.2f, 0f)),
            _ => AnimationCurve.Constant(0f, 1f, 1f)
        };
    }

    private static Dictionary<string, object> SerializeMinMaxGradient(ParticleSystem.MinMaxGradient grad)
    {
        var dict = new Dictionary<string, object>
        {
            ["mode"] = grad.mode.ToString()
        };

        switch (grad.mode)
        {
            case ParticleSystemGradientMode.Color:
                dict["color"] = ColorToHex(grad.color);
                break;
            case ParticleSystemGradientMode.TwoColors:
                dict["colorMin"] = ColorToHex(grad.colorMin);
                dict["colorMax"] = ColorToHex(grad.colorMax);
                break;
            case ParticleSystemGradientMode.Gradient:
            case ParticleSystemGradientMode.RandomColor:
                dict["colorKeys"] = SerializeColorKeys(grad.gradient);
                dict["alphaKeys"] = SerializeAlphaKeys(grad.gradient);
                break;
            case ParticleSystemGradientMode.TwoGradients:
                dict["colorKeysMin"] = SerializeColorKeys(grad.gradientMin);
                dict["alphaKeysMin"] = SerializeAlphaKeys(grad.gradientMin);
                dict["colorKeysMax"] = SerializeColorKeys(grad.gradientMax);
                dict["alphaKeysMax"] = SerializeAlphaKeys(grad.gradientMax);
                break;
        }
        return dict;
    }

    private static List<object> SerializeColorKeys(Gradient g)
    {
        var list = new List<object>();
        if (g == null || g.colorKeys == null) return list;
        foreach (var ck in g.colorKeys)
        {
            list.Add(new Dictionary<string, object>
            {
                ["time"] = (double)ck.time,
                ["color"] = ColorToHex(ck.color)
            });
        }
        return list;
    }

    private static List<object> SerializeAlphaKeys(Gradient g)
    {
        var list = new List<object>();
        if (g == null || g.alphaKeys == null) return list;
        foreach (var ak in g.alphaKeys)
        {
            list.Add(new Dictionary<string, object>
            {
                ["time"] = (double)ak.time,
                ["alpha"] = (double)ak.alpha
            });
        }
        return list;
    }

    private static ParticleSystem.MinMaxGradient ParseMinMaxGradient(object val, ParticleSystem.MinMaxGradient fallback)
    {
        if (val is string hexStr)
        {
            return new ParticleSystem.MinMaxGradient(ParseColor(hexStr, Color.white));
        }

        if (val is IList list && list.Count >= 3 && (list[0] is double || list[0] is long))
        {
            return new ParticleSystem.MinMaxGradient(ParseColor(val, Color.white));
        }

        if (val is Dictionary<string, object> d)
        {
            string modeStr = GetString(d, "mode", null);
            if (string.IsNullOrEmpty(modeStr))
            {
                if (d.ContainsKey("colorKeys") || d.ContainsKey("alphaKeys")) modeStr = "Gradient";
                else if (d.ContainsKey("colorMin") || d.ContainsKey("colorMax")) modeStr = "TwoColors";
                else modeStr = "Color";
            }

            switch (modeStr)
            {
                case "Color":
                    return new ParticleSystem.MinMaxGradient(
                        d.TryGetValue("color", out var cObj) ? ParseColor(cObj, Color.white) : Color.white);
                case "TwoColors":
                    return new ParticleSystem.MinMaxGradient(
                        d.TryGetValue("colorMin", out var cMin) ? ParseColor(cMin, Color.white) : Color.white,
                        d.TryGetValue("colorMax", out var cMax) ? ParseColor(cMax, Color.white) : Color.white);
                case "Gradient":
                case "RandomColor":
                {
                    var g = ParseGradient(d, fallback.gradient);
                    var mmg = new ParticleSystem.MinMaxGradient(g);
                    if (modeStr == "RandomColor") mmg.mode = ParticleSystemGradientMode.RandomColor;
                    return mmg;
                }
            }
        }

        return fallback;
    }

    private static Gradient ParseGradient(Dictionary<string, object> d, Gradient existing)
    {
        var g = new Gradient();
        GradientColorKey[] cKeys = existing != null && existing.colorKeys != null && existing.colorKeys.Length > 0
            ? existing.colorKeys
            : new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };

        GradientAlphaKey[] aKeys = existing != null && existing.alphaKeys != null && existing.alphaKeys.Length > 0
            ? existing.alphaKeys
            : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) };

        if (d.TryGetValue("colorKeys", out var ckObj) && ckObj is IList ckList && ckList.Count > 0)
        {
            var list = new List<GradientColorKey>();
            foreach (var item in ckList)
            {
                if (item is Dictionary<string, object> kd)
                {
                    float t = GetFloat(kd, "time", 0f);
                    Color c = kd.TryGetValue("color", out var colVal) ? ParseColor(colVal, Color.white) : Color.white;
                    list.Add(new GradientColorKey(c, t));
                }
            }
            if (list.Count > 0) cKeys = list.ToArray();
        }

        if (d.TryGetValue("alphaKeys", out var akObj) && akObj is IList akList && akList.Count > 0)
        {
            var list = new List<GradientAlphaKey>();
            foreach (var item in akList)
            {
                if (item is Dictionary<string, object> kd)
                {
                    float t = GetFloat(kd, "time", 0f);
                    float a = GetFloat(kd, "alpha", 1f);
                    list.Add(new GradientAlphaKey(a, t));
                }
            }
            if (list.Count > 0) aKeys = list.ToArray();
        }

        g.SetKeys(cKeys, aKeys);
        return g;
    }

    private static ParticleSystem.MinMaxCurve MultiplyMinMaxCurve(ParticleSystem.MinMaxCurve c, float factor)
    {
        switch (c.mode)
        {
            case ParticleSystemCurveMode.Constant:
                c.constant *= factor;
                break;
            case ParticleSystemCurveMode.TwoConstants:
                c.constantMin *= factor;
                c.constantMax *= factor;
                break;
            case ParticleSystemCurveMode.Curve:
            case ParticleSystemCurveMode.TwoCurves:
                c.curveMultiplier *= factor;
                break;
        }
        return c;
    }

    private static float GetCurveMax(ParticleSystem.MinMaxCurve c)
    {
        return c.mode switch
        {
            ParticleSystemCurveMode.Constant => c.constant,
            ParticleSystemCurveMode.TwoConstants => Mathf.Max(c.constantMin, c.constantMax),
            _ => c.curveMultiplier
        };
    }

    private static ParticleSystem.MinMaxGradient RecolorMinMaxGradient(
        ParticleSystem.MinMaxGradient grad,
        bool useTargetPalette,
        Color primary,
        Color secondary,
        float hueShiftDeg,
        float satMult,
        float valMult)
    {
        switch (grad.mode)
        {
            case ParticleSystemGradientMode.Color:
                grad.color = TransformColor(grad.color, useTargetPalette, primary, secondary, 0f, hueShiftDeg, satMult, valMult);
                break;
            case ParticleSystemGradientMode.TwoColors:
                grad.colorMin = TransformColor(grad.colorMin, useTargetPalette, primary, secondary, 0f, hueShiftDeg, satMult, valMult);
                grad.colorMax = TransformColor(grad.colorMax, useTargetPalette, primary, secondary, 1f, hueShiftDeg, satMult, valMult);
                break;
            case ParticleSystemGradientMode.Gradient:
            case ParticleSystemGradientMode.RandomColor:
                if (grad.gradient != null)
                {
                    var g = new Gradient();
                    var cks = grad.gradient.colorKeys;
                    for (int i = 0; i < cks.Length; i++)
                    {
                        cks[i].color = TransformColor(cks[i].color, useTargetPalette, primary, secondary, cks[i].time, hueShiftDeg, satMult, valMult);
                    }
                    g.SetKeys(cks, grad.gradient.alphaKeys);
                    grad.gradient = g;
                }
                break;
        }
        return grad;
    }

    private static Color TransformColor(
        Color orig,
        bool useTargetPalette,
        Color primary,
        Color secondary,
        float normalizedTime,
        float hueShiftDeg,
        float satMult,
        float valMult)
    {
        float alpha = orig.a;
        Color.RGBToHSV(orig, out float h, out float s, out float v);

        if (useTargetPalette)
        {
            Color targetTone = Color.Lerp(primary, secondary, Mathf.Clamp01(normalizedTime));
            Color.RGBToHSV(targetTone, out float th, out float ts, out _);
            h = th;
            s = Mathf.Clamp01(s < 0.08f ? ts * 0.35f : ts);
        }

        if (Mathf.Abs(hueShiftDeg) > 0.01f)
        {
            h = (h + (hueShiftDeg / 360f)) % 1f;
            if (h < 0f) h += 1f;
        }

        s = Mathf.Clamp01(s * satMult);
        v = Mathf.Max(0f, v * valMult);

        Color result = Color.HSVToRGB(h, s, v, true);
        result.a = alpha;
        return result;
    }

    #endregion

    #region Target Resolution & Execution Helpers

    private delegate object TargetAction(GameObject root, bool isPrefab);

    private static object ExecuteOnTarget(string prefabPath, long instanceId, TargetAction action)
    {
        if (!string.IsNullOrEmpty(prefabPath))
        {
            if (!File.Exists(prefabPath) && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                return Error($"Prefab not found at path: '{prefabPath}'");

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return Error($"Failed to load prefab contents at '{prefabPath}'");

            try
            {
                var res = action(root, true);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                return res;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (instanceId != 0)
        {
            GameObject sceneGo = FindSceneGameObject(instanceId);
            if (sceneGo == null)
                return Error($"Scene GameObject with instanceId {instanceId} not found.");

            var res = action(sceneGo, false);
            EditorUtility.SetDirty(sceneGo);
            EditorSceneManager.MarkSceneDirty(sceneGo.scene);
            return res;
        }

        return Error("Missing target identifier: provide either 'prefabPath' or 'instanceId'.");
    }

    private static GameObject ResolveReadOnlyTarget(string prefabPath, long instanceId, out bool isPrefabAsset, out string resolvedPath)
    {
        isPrefabAsset = false;
        resolvedPath = prefabPath;

        if (!string.IsNullOrEmpty(prefabPath))
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (asset != null)
            {
                isPrefabAsset = true;
                return asset;
            }
        }

        if (instanceId != 0)
        {
            var go = FindSceneGameObject(instanceId);
            if (go != null)
            {
                resolvedPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                return go;
            }
        }

        return null;
    }

    private static GameObject FindSceneGameObject(long instanceId)
    {
        var obj = EditorUtility.InstanceIDToObject((int)instanceId) as GameObject;
        if (obj != null) return obj;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindByIdRecursive(root, (int)instanceId);
                if (found != null) return found;
            }
        }
        return null;
    }

    private static GameObject FindByIdRecursive(GameObject go, int id)
    {
        if (go.GetInstanceID() == id) return go;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            var f = FindByIdRecursive(go.transform.GetChild(i).gameObject, id);
            if (f != null) return f;
        }
        return null;
    }

    private static ParticleSystem FindParticleSystemByNodePath(GameObject root, string nodePath)
    {
        Transform t = FindTransformByNodePath(root.transform, nodePath);
        return t != null ? t.GetComponent<ParticleSystem>() : null;
    }

    private static Transform FindTransformByNodePath(Transform root, string nodePath)
    {
        if (string.IsNullOrEmpty(nodePath) || nodePath == "." || nodePath == "/" ||
            string.Equals(nodePath, root.name, StringComparison.OrdinalIgnoreCase))
            return root;

        // Strip leading "Root/" if caller included root name
        string normalized = nodePath.TrimStart('/');
        if (normalized.StartsWith(root.name + "/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(root.name.Length + 1);

        Transform direct = root.Find(normalized);
        if (direct != null) return direct;

        // Fallback search by leaf name (case-insensitive)
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (string.Equals(t.name, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetRelativePath(root, t), normalized, StringComparison.OrdinalIgnoreCase))
                return t;
        }
        return null;
    }

    private static string GetRelativePath(Transform root, Transform child)
    {
        if (child == root) return ".";
        var parts = new List<string>();
        Transform curr = child;
        while (curr != null && curr != root)
        {
            parts.Add(curr.name);
            curr = curr.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static bool PathMatches(string relPath, string filter, string name)
    {
        if (filter == "." && relPath == ".") return true;
        return string.Equals(relPath, filter, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, filter, StringComparison.OrdinalIgnoreCase);
    }

    private static Mesh ResolveMesh(string pathOrPrimitive)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathOrPrimitive);
        if (mesh != null) return mesh;

        // Support Unity built-in meshes like "Sphere", "Cube", "Quad", "Cylinder", "Capsule"
        if (Enum.TryParse<PrimitiveType>(pathOrPrimitive, true, out var prim))
        {
            var temp = GameObject.CreatePrimitive(prim);
            var sharedMesh = temp.GetComponent<MeshFilter>()?.sharedMesh;
            Object.DestroyImmediate(temp);
            return sharedMesh;
        }

        return null;
    }

    private static void CaptureCameraToPng(Camera cam, string outputPath, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        RenderTexture prevTarget = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    #endregion

    #region Primitive Conversion Helpers

    private static Dictionary<string, object> Error(string message)
    {
        return new Dictionary<string, object>
        {
            ["success"] = false,
            ["error"] = message
        };
    }

    private static string GetString(Dictionary<string, object> d, string key, string fallback)
    {
        return d != null && d.TryGetValue(key, out var v) && v != null ? v.ToString() : fallback;
    }

    private static float GetFloat(Dictionary<string, object> d, string key, float fallback)
    {
        if (d == null || !d.TryGetValue(key, out var v) || v == null) return fallback;
        return Convert.ToSingle(v, CultureInfo.InvariantCulture);
    }

    private static int GetInt(Dictionary<string, object> d, string key, int fallback)
    {
        if (d == null || !d.TryGetValue(key, out var v) || v == null) return fallback;
        return Convert.ToInt32(v, CultureInfo.InvariantCulture);
    }

    private static long GetLong(Dictionary<string, object> d, string key, long fallback)
    {
        if (d == null || !d.TryGetValue(key, out var v) || v == null) return fallback;
        return Convert.ToInt64(v, CultureInfo.InvariantCulture);
    }

    private static bool GetBool(Dictionary<string, object> d, string key, bool fallback)
    {
        if (d == null || !d.TryGetValue(key, out var v) || v == null) return fallback;
        return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
    }

    private static List<object> Vec3ToList(Vector3 v)
    {
        return new List<object>
        {
            Math.Round(v.x, 4),
            Math.Round(v.y, 4),
            Math.Round(v.z, 4)
        };
    }

    private static Vector3 ParseVec3(object obj, Vector3 fallback)
    {
        if (obj is IList list && list.Count >= 3)
        {
            return new Vector3(
                Convert.ToSingle(list[0], CultureInfo.InvariantCulture),
                Convert.ToSingle(list[1], CultureInfo.InvariantCulture),
                Convert.ToSingle(list[2], CultureInfo.InvariantCulture));
        }
        if (obj is Dictionary<string, object> d)
        {
            return new Vector3(
                GetFloat(d, "x", fallback.x),
                GetFloat(d, "y", fallback.y),
                GetFloat(d, "z", fallback.z));
        }
        return fallback;
    }

    private static string ColorToHex(Color c)
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(c);
    }

    private static Color ParseColor(object obj, Color fallback)
    {
        if (obj is string s)
        {
            string clean = s.StartsWith("#") ? s : "#" + s;
            if (ColorUtility.TryParseHtmlString(clean, out var parsed))
                return parsed;
        }
        else if (obj is IList list && list.Count >= 3)
        {
            return new Color(
                Convert.ToSingle(list[0], CultureInfo.InvariantCulture),
                Convert.ToSingle(list[1], CultureInfo.InvariantCulture),
                Convert.ToSingle(list[2], CultureInfo.InvariantCulture),
                list.Count >= 4 ? Convert.ToSingle(list[3], CultureInfo.InvariantCulture) : 1f);
        }
        else if (obj is Dictionary<string, object> d)
        {
            return new Color(
                GetFloat(d, "r", fallback.r),
                GetFloat(d, "g", fallback.g),
                GetFloat(d, "b", fallback.b),
                GetFloat(d, "a", fallback.a));
        }
        return fallback;
    }

    private static bool TryParseEnum<TEnum>(object val, out TEnum result) where TEnum : struct
    {
        if (val != null && Enum.TryParse(val.ToString(), true, out result))
            return true;
        result = default;
        return false;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    #endregion
}
#endif

