// Veilheim

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Veilheim.PatchEvents.PatchStubs;

namespace Veilheim.PatchEvents
{
    /// <summary>
    /// Dispatcher to register events in their respective Harmony Patch class
    /// </summary>
    public class PatchDispatcher
    {
        public static PatchDispatcher Instance;

        private MethodInfo addHarmonyEvent;

        public PatchDispatcher()
        {
            try
            {
                // Cache the AddHarmonyEvent methodinfo
                addHarmonyEvent = this.GetType().GetMethod(nameof(AddHarmonyEvent), BindingFlags.Static | BindingFlags.NonPublic);

                // Get list of all Payload methods
                List<Tuple<MethodInfo, PatchEventAttribute>> payloadMethods = GetPayloadMethods().ToList();

                // Get list of all Harmony Patch classes
                List<Tuple<Type, HarmonyPatch>> patchClasses = GetPatchClasses().ToList();

                // Register events in their respective harmony patch classes
                foreach (var patchClass in patchClasses)
                {
                    foreach (var payload in payloadMethods.Where(x =>
                            (x.Item2.ClassToPatch == patchClass.Item2.info.declaringType) && (x.Item2.MethodName == patchClass.Item2.info.methodName))
                        .OrderBy(x => x.Item2.Priority).ToList())
                    {
                        Logger.LogInfo($"Patching class {patchClass.Item2.info.declaringType.Name}.{patchClass.Item2.info.methodName} with method {payload.Item1.DeclaringType.Name}.{payload.Item1.Name}");

                        // make method generic (harmony patch class) and invoke it
                        var generic = addHarmonyEvent.MakeGenericMethod(patchClass.Item1);
                        generic.Invoke(null, new object[] { payload.Item1, payload.Item2.EventType });

                        // Remove from original list
                        payloadMethods.Remove(payload);
                    }
                }

                // Show error log if there are unprocessed payload methods
                foreach (var payload in payloadMethods)
                {
                    Logger.LogError($"Patch method {payload.Item1.DeclaringType}.{payload.Item1.Name} for {payload.Item2.ClassToPatch.Name}.{payload.Item2.MethodName} was not used. Is the patch class missing?");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        /// <summary>
        /// Get list of all methods in classes derived from Payload
        /// with an attached PatchEvent attribute
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Tuple<MethodInfo, PatchEventAttribute>> GetPayloadMethods()
        {
            foreach (var type in this.GetType().Assembly.GetTypes().Where(x => x.BaseType == typeof(Payload)))
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .Where(x => x.GetCustomAttribute<PatchEventAttribute>() != null))
                {
                    yield return new Tuple<MethodInfo, PatchEventAttribute>(method, method.GetCustomAttribute<PatchEventAttribute>());
                }
            }
        }

        /// <summary>
        /// Get list of all classes with an attached HarmonyPatch attribute
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Tuple<Type, HarmonyPatch>> GetPatchClasses()
        {
            foreach (var type in this.GetType().Assembly.GetTypes().Where(x => x.GetCustomAttribute<HarmonyPatch>() != null))
            {
                yield return new Tuple<Type, HarmonyPatch>(type, type.GetCustomAttribute<HarmonyPatch>());
            }
        }

        /// <summary>
        /// Generic method to add an event to an eventhandler inside of a harmony patch class
        /// </summary>
        /// <typeparam name="T">Type of Harmony Patch class</typeparam>
        /// <param name="method">Method to add to eventhandler</param>
        /// <param name="eventType">BlockingPrefix,Prefix or Postfix</param>
        private static void AddHarmonyEvent<T>(MethodInfo method, PatchEventType eventType) where T : class
        {
            try
            {
                EventInfo evt;
                // Get eventhandler according to PatchEventType
                if (eventType == PatchEventType.BlockingPrefix)
                {
                    evt = typeof(T).GetEvents().First(x => x.Name == nameof(ZNet_Awake_Patch.BlockingPrefixEvent));
                }
                else if (eventType == PatchEventType.Prefix)
                {
                    evt = typeof(T).GetEvents().First(x => x.Name == nameof(ZNet_Awake_Patch.PrefixEvent));
                }
                else
                {
                    evt = typeof(T).GetEvents().First(x => x.Name == nameof(ZNet_Awake_Patch.PostfixEvent));
                }

                // Just in case if the HarmonyPatch class has no eventhandler
                if (evt == null)
                {
                    Logger.LogError($"{eventType} Event could not be found for class {method.DeclaringType}");
                }


                Type eventHandlerType = evt.EventHandlerType;

                // Create delegate
                Delegate @delegate = Delegate.CreateDelegate(eventHandlerType, method);

                // Add to event handler
                MethodInfo addMethod = evt.GetAddMethod();
                addMethod.Invoke(null, new object[] { @delegate });
            }
            // Will throw error if method parameters are not in line with the eventhandler's delegate
            catch (Exception ex)
            {
                Logger.LogError(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public static void Init()
        {
            Instance = new PatchDispatcher();
            Logger.LogInfo("Patchdispatcher activated");
        }
    }
}