using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TP4.EditorTools
{
    /// <summary>
    /// Genera todos los ScriptableObjects del juego (estados, módulos, ítems, robots, encuentros, torneo)
    /// y la escena de combate lista para darle Play.
    /// Menú: TP4 > 1. Crear contenido y escena de combate.
    /// Si un asset ya existe NO se sobrescribe (para no pisar tu balance). Para regenerar todo usá la opción 2.
    /// </summary>
    public static class TP4SetupWizard
    {
        const string Raiz = "Assets/_TP4";
        const string Datos = Raiz + "/Data";
        const string Escena = Raiz + "/Scenes/Combate.unity";
        const string EscenaMenu = Raiz + "/Scenes/MenuPrincipal.unity";

        [MenuItem("TP4/1. Crear contenido y escena de combate", priority = 1)]
        public static void CrearTodo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CrearCarpetas();
            CrearVisuales();
            CrearDatos();
            // Guardar ANTES de crear la escena: NewScene descarga los assets en memoria
            // y las referencias (y cambios sin guardar) se perderían.
            AssetDatabase.SaveAssets();
            CrearEscena();
            CrearEscenaMenu();
            ConfigurarBuild();
            EditorSceneManager.OpenScene(EscenaMenu);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TP4] Listo: contenido y escena de combate creados. Dale Play en " + Escena);
        }

        [MenuItem("TP4/2. Regenerar datos (sobrescribe balance)", priority = 2)]
        public static void Regenerar()
        {
            if (!EditorUtility.DisplayDialog("Regenerar datos",
                    "Se borra la carpeta Assets/_TP4/Data y se vuelve a crear con los valores por defecto. ¿Seguro?",
                    "Regenerar", "Cancelar")) return;
            AssetDatabase.DeleteAsset(Datos);
            CrearTodo();
        }

        [MenuItem("TP4/4. Abrir menú principal", priority = 4)]
        public static void AbrirMenu()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(EscenaMenu);
        }

        [MenuItem("TP4/3. Abrir escena de combate", priority = 3)]
        public static void AbrirEscena()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Escena);
        }

        // =====================================================================
        // Carpetas y helpers
        // =====================================================================
        static void CrearCarpetas()
        {
            foreach (var c in new[]
                     {
                         Raiz, Datos, Datos + "/Estados", Datos + "/Habilidades", Datos + "/Items", Datos + "/Robots",
                         Datos + "/Encuentros", Raiz + "/Materials", Raiz + "/Scenes", Raiz + "/Art", Raiz + "/Audio"
                     })
            {
                if (AssetDatabase.IsValidFolder(c)) continue;
                string padre = Path.GetDirectoryName(c).Replace('\\', '/');
                AssetDatabase.CreateFolder(padre, Path.GetFileName(c));
            }
        }

        /// <summary>Carga el asset si existe; si no, lo crea y lo llena.</summary>
        static T Asset<T>(string ruta, Action<T> llenar) where T : ScriptableObject
        {
            var existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente != null) return existente;
            var nuevo = ScriptableObject.CreateInstance<T>();
            llenar(nuevo);
            AssetDatabase.CreateAsset(nuevo, ruta);
            return nuevo;
        }

        static Material Mat(string nombre, string shader, Color color, bool emisivo = false)
        {
            string ruta = $"{Raiz}/Materials/{nombre}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (m != null) return m;
            var sh = Shader.Find(shader);
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(sh) { color = color };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (emisivo && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 2f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(m, ruta);
            return m;
        }

        // ---------- Efectos ----------
        static ActionEffect Fis(float pot) => new ActionEffect { tipo = EffectType.DanoFisico, potencia = pot };
        static ActionEffect Ene(float pot) => new ActionEffect { tipo = EffectType.DanoEnergia, potencia = pot };
        static ActionEffect Fijo(float v) => new ActionEffect { tipo = EffectType.DanoFijo, potencia = 0, valorFijo = v };
        static ActionEffect Cura(float pot, float fijo) => new ActionEffect { tipo = EffectType.Curar, potencia = pot, valorFijo = fijo };
        static ActionEffect Energia(float fijo) => new ActionEffect { tipo = EffectType.RestaurarEnergia, potencia = 0, valorFijo = fijo };
        static ActionEffect Limpiar() => new ActionEffect { tipo = EffectType.LimpiarEstados };
        static ActionEffect Atb(float cant) => new ActionEffect { tipo = EffectType.ModificarATB, potencia = cant };
        static ActionEffect Provocar(float dur) => new ActionEffect { tipo = EffectType.Provocar, duracion = dur };

        static ActionEffect Stat(StatType st, float pct, float dur) => new ActionEffect
        {
            tipo = EffectType.ModificarStat,
            modificador = new StatModifierDef { stat = st, porcentaje = pct },
            duracion = dur
        };

        static ActionEffect Estado(StatusData e, float prob) => new ActionEffect
        {
            tipo = EffectType.AplicarEstado, estado = e, probabilidad = prob
        };

        static AbilityData Hab(string archivo, string nombre, string desc, TargetMode obj, int costo, float recup,
            ActionAnim anim, bool melee, Color color, params ActionEffect[] efectos)
        {
            return Asset<AbilityData>($"{Datos}/Habilidades/{archivo}.asset", h =>
            {
                h.nombre = nombre;
                h.descripcion = desc;
                h.objetivo = obj;
                h.costoEnergia = costo;
                h.recuperacion = recup;
                h.animacion = anim;
                h.cuerpoACuerpo = melee;
                h.colorEfecto = color;
                h.efectos = new List<ActionEffect>(efectos);
            });
        }

        static AbilityData Overdrive(string archivo, string nombre, string desc, TargetMode obj, bool melee, Color color, params ActionEffect[] efectos)
        {
            return Asset<AbilityData>($"{Datos}/Habilidades/{archivo}.asset", h =>
            {
                h.nombre = nombre;
                h.descripcion = desc;
                h.objetivo = obj;
                h.esOverdrive = true;
                h.recuperacion = 0.8f;
                h.animacion = ActionAnim.Habilidad;
                h.cuerpoACuerpo = melee;
                h.colorEfecto = color;
                h.esCinematica = true;
                h.duracionCinematica = 2.6f;
                h.efectos = new List<ActionEffect>(efectos);
            });
        }

        static ItemData Item(string archivo, string nombre, string desc, TargetMode obj, Color color, params ActionEffect[] efectos)
        {
            return Asset<ItemData>($"{Datos}/Items/{archivo}.asset", it =>
            {
                it.nombre = nombre;
                it.descripcion = desc;
                it.objetivo = obj;
                it.recuperacion = 0.8f;
                it.animacion = ActionAnim.Item;
                it.colorEfecto = color;
                it.efectos = new List<ActionEffect>(efectos);
            });
        }

        // ---------- Traducciones ----------
        static void EN(ActionData a, string nombreEN, string descEN)
        {
            if (a == null) return;
            bool cambio = false;
            if (string.IsNullOrEmpty(a.nombreEN)) { a.nombreEN = nombreEN; cambio = true; }
            if (string.IsNullOrEmpty(a.descripcionEN) && !string.IsNullOrEmpty(descEN)) { a.descripcionEN = descEN; cambio = true; }
            if (cambio) EditorUtility.SetDirty(a);
        }

        static void ENEstado(StatusData e, string nombreEN, string abrevEN)
        {
            if (e == null || !string.IsNullOrEmpty(e.nombreEN)) return;
            e.nombreEN = nombreEN;
            e.abreviaturaEN = abrevEN;
            EditorUtility.SetDirty(e);
        }

        static void ENRobot(RobotData r, string descEN)
        {
            if (r == null || !string.IsNullOrEmpty(r.descripcionEN)) return;
            r.descripcionEN = descEN;
            EditorUtility.SetDirty(r);
        }

        static void ENEncuentro(EncounterData e, string nombreEN, string rivalEN)
        {
            if (e == null || !string.IsNullOrEmpty(e.nombreEN)) return;
            e.nombreEN = nombreEN;
            e.nombreRivalEN = rivalEN;
            EditorUtility.SetDirty(e);
        }

        static RobotData Robot(string archivo, string nombre, RobotRole rol, string desc, Color color, Stats stats, int energia,
            AbilityData basico, AbilityData overdrive, float escala, params AbilityData[] habilidades)
        {
            return Asset<RobotData>($"{Datos}/Robots/{archivo}.asset", r =>
            {
                r.nombre = nombre;
                r.rol = rol;
                r.descripcion = desc;
                r.colorPrincipal = color;
                r.stats = stats;
                r.energiaMax = energia;
                r.ataqueBasico = basico;
                r.overdrive = overdrive;
                r.escala = escala;
                r.habilidades = new List<AbilityData>(habilidades);
            });
        }

        // =====================================================================
        // Visuales
        // =====================================================================
        static BattleVisuals CrearVisuales()
        {
            var lit = Mat("MAT_Placeholder", "Universal Render Pipeline/Lit", Color.gray);
            var emisivo = Mat("MAT_Emisivo", "Universal Render Pipeline/Unlit", Color.white, true);
            var trail = Mat("MAT_Trail", "Universal Render Pipeline/Particles/Unlit", Color.white, true);
            var piso = Mat("MAT_Piso", "Universal Render Pipeline/Lit", new Color(0.12f, 0.13f, 0.17f));

            return Asset<BattleVisuals>($"{Raiz}/BattleVisuals.asset", v =>
            {
                v.materialPlaceholder = lit;
                v.materialEmisivo = emisivo;
                v.materialTrail = trail;
                v.materialPiso = piso;
            });
        }

        // =====================================================================
        // Datos de juego
        // =====================================================================
        static TournamentData CrearDatos()
        {
            // ---------- Estados ----------
            var quemadura = Asset<StatusData>($"{Datos}/Estados/EST_Quemadura.asset", s =>
            {
                s.nombre = "Quemadura"; s.abreviatura = "QUE"; s.color = new Color(1f, 0.5f, 0.15f);
                s.duracion = 8f; s.intervaloTick = 2f; s.danoPorTickPct = 0.05f;
            });
            var paralisis = Asset<StatusData>($"{Datos}/Estados/EST_Paralisis.asset", s =>
            {
                s.nombre = "Parálisis"; s.abreviatura = "PAR"; s.color = new Color(1f, 0.95f, 0.3f);
                s.duracion = 4f; s.congelaATB = true;
            });
            var corrosion = Asset<StatusData>($"{Datos}/Estados/EST_Corrosion.asset", s =>
            {
                s.nombre = "Corrosión"; s.abreviatura = "COR"; s.color = new Color(0.45f, 1f, 0.35f);
                s.duracion = 12f; s.intervaloTick = 3f; s.danoPorTickPct = 0.03f;
                s.modificadores = new List<StatModifierDef> { new StatModifierDef { stat = StatType.Defensa, porcentaje = -0.25f } };
            });

            Color cBlanco = new Color(0.9f, 0.95f, 1f);
            Color cNaranja = new Color(1f, 0.55f, 0.15f);
            Color cAmarillo = new Color(1f, 0.95f, 0.3f);
            Color cVerde = new Color(0.45f, 1f, 0.4f);
            Color cVioleta = new Color(0.75f, 0.4f, 1f);
            Color cCian = new Color(0.3f, 0.9f, 1f);

            // ---------- Ataques básicos ----------
            var golpe = Hab("HAB_Golpe", "Golpe", "Ataque cuerpo a cuerpo básico.", TargetMode.EnemigoUnico, 0, 1f, ActionAnim.Ataque, true, cBlanco, Fis(1f));
            var disparo = Hab("HAB_Disparo", "Disparo", "Disparo básico a distancia.", TargetMode.EnemigoUnico, 0, 1f, ActionAnim.Disparo, false, cCian, Fis(0.9f));

            // ---------- Bastión (Tanque) ----------
            var provocar = Hab("HAB_Provocar", "Provocar", "Atrae los ataques enemigos durante 12 s y sube su Defensa.", TargetMode.Propio, 8, 0.7f, ActionAnim.Habilidad, false, cNaranja,
                Provocar(12f), Stat(StatType.Defensa, 0.3f, 12f));
            var escudo = Hab("HAB_Escudo", "Escudo", "+50% de Defensa a un aliado durante 20 s.", TargetMode.AliadoUnico, 10, 1f, ActionAnim.Habilidad, false, cCian,
                Stat(StatType.Defensa, 0.5f, 20f));
            var pulso = Hab("HAB_PulsoChoque", "Pulso de choque", "Daño de energía que además retrasa la ATB del objetivo.", TargetMode.EnemigoUnico, 12, 1f, ActionAnim.Disparo, false, cCian,
                Ene(1.2f), Atb(-0.25f));
            var muralla = Overdrive("OD_Muralla", "Muralla", "Todo el equipo gana +60% de Defensa y recupera algo de vida.", TargetMode.TodosAliados, false, cCian,
                Stat(StatType.Defensa, 0.6f, 25f), Cura(0.5f, 40f));

            // ---------- Vanguardia (Asalto) ----------
            var cargado = Hab("HAB_GolpeCargado", "Golpe cargado", "Golpe devastador. Tarda más en volver a actuar.", TargetMode.EnemigoUnico, 10, 1.6f, ActionAnim.Ataque, true, cBlanco,
                Fis(2.2f));
            var rafaga = Hab("HAB_Rafaga", "Ráfaga", "Golpe débil pero muy rápido de recuperar.", TargetMode.EnemigoUnico, 6, 0.55f, ActionAnim.Ataque, true, cBlanco,
                Fis(0.7f));
            var plasma = Hab("HAB_Plasma", "Plasma", "Daño de energía con 80% de causar Quemadura.", TargetMode.EnemigoUnico, 14, 1.1f, ActionAnim.Disparo, false, cNaranja,
                Ene(1.1f), Estado(quemadura, 0.8f));
            var tormenta = Overdrive("OD_TormentaAcero", "Tormenta de acero", "Una ráfaga de golpes demoledora contra un enemigo.", TargetMode.EnemigoUnico, true, cBlanco,
                Fis(4f));

            // ---------- Nova (Artillero) ----------
            var emp = Hab("HAB_EMP", "Descarga EMP", "Daño de energía con 70% de Parálisis (congela la ATB).", TargetMode.EnemigoUnico, 14, 1.1f, ActionAnim.Disparo, false, cAmarillo,
                Ene(0.9f), Estado(paralisis, 0.7f));
            var acido = Hab("HAB_LluviaAcida", "Lluvia ácida", "Daño a todos los enemigos con 60% de Corrosión (−Defensa).", TargetMode.TodosEnemigos, 20, 1.3f, ActionAnim.Disparo, false, cVerde,
                Ene(0.6f), Estado(corrosion, 0.6f));
            var sobrecarga = Hab("HAB_Sobrecarga", "Sobrecarga", "Enorme daño de energía. Recuperación muy lenta.", TargetMode.EnemigoUnico, 24, 1.7f, ActionAnim.Disparo, false, cVioleta,
                Ene(2.4f));
            var orbital = Overdrive("OD_CanonOrbital", "Cañón orbital", "Rayo desde el cielo que golpea a todos los enemigos.", TargetMode.TodosEnemigos, false, cVioleta,
                Ene(2.6f));

            // ---------- Chispa (Mecánico) ----------
            var reparar = Hab("HAB_Reparar", "Reparar", "Recupera vida de un aliado.", TargetMode.AliadoUnico, 12, 0.9f, ActionAnim.Habilidad, false, cVerde,
                Cura(1.6f, 20f));
            var overclock = Hab("HAB_Overclock", "Overclock", "Llena gran parte de la ATB de un aliado y sube su Agilidad.", TargetMode.AliadoUnico, 10, 0.8f, ActionAnim.Habilidad, false, cCian,
                Atb(0.6f), Stat(StatType.Agilidad, 0.3f, 15f));
            var purga = Hab("HAB_Purga", "Purga", "Quita estados alterados y debuffs de un aliado.", TargetMode.AliadoUnico, 8, 0.8f, ActionAnim.Habilidad, false, cBlanco,
                Limpiar());
            var reinicio = Overdrive("OD_ReinicioTotal", "Reinicio total", "Cura a todo el equipo, limpia estados y recarga energía.", TargetMode.TodosAliados, false, cVerde,
                Cura(1.5f, 60f), Limpiar(), Energia(15f));

            // ---------- Jefe ----------
            var puno = Hab("HAB_PunoSismico", "Puño sísmico", "", TargetMode.EnemigoUnico, 0, 1f, ActionAnim.Ataque, true, cNaranja, Fis(1.5f));
            var laser = Hab("HAB_LaserBarrido", "Láser de barrido", "", TargetMode.TodosEnemigos, 15, 1.2f, ActionAnim.Disparo, false, new Color(1f, 0.25f, 0.2f), Ene(0.8f));
            var misiles = Hab("HAB_Misiles", "Lluvia de misiles", "", TargetMode.TodosEnemigos, 20, 1.3f, ActionAnim.Disparo, false, cNaranja, Fis(0.7f), Estado(quemadura, 0.35f));
            var empMasivo = Hab("HAB_EMPMasivo", "Pulso EMP masivo", "", TargetMode.TodosEnemigos, 25, 1.2f, ActionAnim.Habilidad, false, cAmarillo, Ene(0.5f), Estado(paralisis, 0.4f));
            var nucleo = Hab("HAB_NucleoSobrecargado", "Núcleo sobrecargado", "", TargetMode.EnemigoUnico, 30, 1.5f, ActionAnim.Disparo, false, new Color(1f, 0.2f, 0.5f), Ene(2.5f));

            // ---------- Ítems ----------
            var nucleoEnergia = Item("ITEM_NucleoEnergia", "Núcleo de energía", "Recupera 150 de vida a un aliado.", TargetMode.AliadoUnico, cVerde, Cura(0f, 150f));
            var celda = Item("ITEM_CeldaEnergia", "Celda de energía", "Recupera 40 de energía a un aliado.", TargetMode.AliadoUnico, cVioleta, Energia(40f));
            var bomba = Item("ITEM_Bomba", "Bomba", "70 de daño a todos los enemigos y −20% Defensa por 15 s.", TargetMode.TodosEnemigos, cNaranja,
                Fijo(70f), Stat(StatType.Defensa, -0.2f, 15f));
            var pieza = Item("ITEM_PiezaDorada", "Pieza dorada", "+30% de Fuerza a un aliado durante 25 s.", TargetMode.AliadoUnico, cAmarillo,
                Stat(StatType.Fuerza, 0.3f, 25f));

            // ---------- Robots del jugador ----------
            var bastion = Robot("ROB_Bastion", "Bastión", RobotRole.Tanque, "Tanque. Mucha vida y defensa; protege al equipo.", new Color(0.25f, 0.45f, 0.95f),
                new Stats(16, 8, 30, 10, 22), 50, golpe, muralla, 1.1f, provocar, escudo, pulso);
            var vanguardia = Robot("ROB_Vanguardia", "Vanguardia", RobotRole.Asalto, "Asalto. Daño físico alto y rápido.", new Color(0.9f, 0.25f, 0.25f),
                new Stats(24, 18, 20, 10, 12), 45, golpe, tormenta, 1f, cargado, rafaga, plasma);
            var nova = Robot("ROB_Nova", "Nova", RobotRole.Artillero, "Artillero. Módulos de energía y estados alterados.", new Color(0.65f, 0.3f, 0.95f),
                new Stats(8, 14, 16, 26, 8), 90, disparo, orbital, 0.95f, emp, acido, sobrecarga);
            var chispa = Robot("ROB_Chispa", "Chispa", RobotRole.Mecanico, "Mecánico. Repara, acelera y purga al equipo.", new Color(0.95f, 0.75f, 0.2f),
                new Stats(12, 16, 18, 20, 12), 80, disparo, reinicio, 0.95f, reparar, overclock, purga);

            // ---------- Rivales ----------
            var tuerca = Robot("RIV_Tuerca", "Tuerca", RobotRole.Asalto, "", new Color(0.55f, 0.5f, 0.45f),
                new Stats(16, 14, 15, 8, 8), 30, golpe, null, 0.95f, rafaga);
            var remache = Robot("RIV_Remache", "Remache", RobotRole.Tanque, "", new Color(0.45f, 0.45f, 0.5f),
                new Stats(12, 8, 22, 8, 16), 30, golpe, null, 1.05f, provocar);
            var voltio = Robot("RIV_Voltio", "Voltio", RobotRole.Artillero, "", new Color(0.4f, 0.6f, 0.4f),
                new Stats(6, 12, 13, 18, 6), 60, disparo, null, 0.9f, plasma, emp);

            var cromoLanza = Robot("RIV_CromoLanza", "Cromo Lanza", RobotRole.Asalto, "", new Color(0.75f, 0.78f, 0.82f),
                new Stats(22, 17, 20, 10, 12), 45, golpe, null, 1f, cargado, plasma);
            var cromoMedico = Robot("RIV_CromoMedico", "Cromo Médico", RobotRole.Mecanico, "", new Color(0.7f, 0.85f, 0.75f),
                new Stats(10, 15, 18, 20, 12), 80, disparo, null, 0.95f, reparar, purga);
            var cromoEscudo = Robot("RIV_CromoEscudo", "Cromo Escudo", RobotRole.Tanque, "", new Color(0.6f, 0.65f, 0.75f),
                new Stats(14, 9, 28, 10, 20), 50, golpe, null, 1.1f, provocar, escudo);

            var goliat = Robot("JEFE_Goliat", "Goliat Ω", RobotRole.Jefe, "Campeón invicto del torneo.", new Color(0.35f, 0.32f, 0.3f),
                new Stats(26, 22, 80, 22, 18), 300, puno, null, 2.2f, laser, misiles);
            if (goliat.fases == null || goliat.fases.Count == 0)
            {
                goliat.fases = new List<BossPhase>
                {
                    new BossPhase { mensaje = "¡Goliat entra en modo combate!", umbralVida = 0.6f, multiplicadorVelocidad = 1.3f,
                        habilidadesNuevas = new List<AbilityData> { empMasivo }, colorTinte = new Color(1f, 0.55f, 0.2f) },
                    new BossPhase { mensaje = "¡NÚCLEO AL LÍMITE!", umbralVida = 0.3f, multiplicadorVelocidad = 1.3f,
                        habilidadesNuevas = new List<AbilityData> { nucleo }, colorTinte = new Color(1f, 0.2f, 0.2f) }
                };
                goliat.azarIA = 0.15f;
                EditorUtility.SetDirty(goliat);
            }

            // ---------- Traducciones al inglés (solo se completan si están vacías) ----------
            EN(golpe, "Strike", "Basic melee attack.");
            EN(disparo, "Shot", "Basic ranged shot.");
            EN(provocar, "Taunt", "Draws enemy attacks for 12 s and raises Defense.");
            EN(escudo, "Shield", "+50% Defense to an ally for 20 s.");
            EN(pulso, "Shock pulse", "Energy damage that also delays the target's ATB.");
            EN(muralla, "Bulwark", "The whole team gains +60% Defense and recovers some HP.");
            EN(cargado, "Charged strike", "Devastating blow. Takes longer to act again.");
            EN(rafaga, "Flurry", "Weak hit, but very quick to recover.");
            EN(plasma, "Plasma", "Energy damage with an 80% chance to Burn.");
            EN(tormenta, "Steel storm", "A crushing barrage of blows on one enemy.");
            EN(emp, "EMP blast", "Energy damage with a 70% chance to Paralyze (freezes ATB).");
            EN(acido, "Acid rain", "Damages all enemies with a 60% chance of Corrosion (−Defense).");
            EN(sobrecarga, "Overload", "Huge energy damage. Very slow recovery.");
            EN(orbital, "Orbital cannon", "A beam from the sky that hits all enemies.");
            EN(reparar, "Repair", "Restores an ally's HP.");
            EN(overclock, "Overclock", "Fills most of an ally's ATB and raises their Agility.");
            EN(purga, "Purge", "Removes status ailments and debuffs from an ally.");
            EN(reinicio, "Full reboot", "Heals the whole team, removes ailments and restores energy.");
            EN(puno, "Seismic punch", "");
            EN(laser, "Sweeping laser", "");
            EN(misiles, "Missile rain", "");
            EN(empMasivo, "Mass EMP pulse", "");
            EN(nucleo, "Overloaded core", "");
            EN(nucleoEnergia, "Energy core", "Restores 150 HP to an ally.");
            EN(celda, "Energy cell", "Restores 40 energy to an ally.");
            EN(bomba, "Bomb", "70 damage to all enemies and −20% Defense for 15 s.");
            EN(pieza, "Golden part", "+30% Strength to an ally for 25 s.");
            ENEstado(quemadura, "Burn", "BRN");
            ENEstado(paralisis, "Paralysis", "PAR");
            ENEstado(corrosion, "Corrosion", "COR");
            ENRobot(bastion, "Tank. High HP and defense; protects the team.");
            ENRobot(vanguardia, "Assault. High and fast physical damage.");
            ENRobot(nova, "Artillery. Energy modules and status ailments.");
            ENRobot(chispa, "Mechanic. Repairs, speeds up and cleanses the team.");
            ENRobot(goliat, "Undefeated tournament champion.");
            if (goliat.fases != null && goliat.fases.Count >= 2)
            {
                if (string.IsNullOrEmpty(goliat.fases[0].mensajeEN) || goliat.fases[0].mensajeEN == "The boss is enraged!")
                    goliat.fases[0].mensajeEN = "Goliath enters combat mode!";
                if (string.IsNullOrEmpty(goliat.fases[1].mensajeEN) || goliat.fases[1].mensajeEN == "The boss is enraged!")
                    goliat.fases[1].mensajeEN = "CORE AT THE LIMIT!";
                EditorUtility.SetDirty(goliat);
            }

            // ---------- Encuentros ----------
            var enc1 = Asset<EncounterData>($"{Datos}/Encuentros/ENC_1_Chatarreros.asset", e =>
            {
                e.nombre = "Ronda 1"; e.nombreRival = "Los Chatarreros";
                e.enemigos = new List<RobotData> { tuerca, remache, voltio };
            });
            var enc2 = Asset<EncounterData>($"{Datos}/Encuentros/ENC_2_EscuadronCromo.asset", e =>
            {
                e.nombre = "Semifinal"; e.nombreRival = "Escuadrón Cromo";
                e.enemigos = new List<RobotData> { cromoLanza, cromoMedico, cromoEscudo };
            });
            var enc3 = Asset<EncounterData>($"{Datos}/Encuentros/ENC_3_Goliat.asset", e =>
            {
                e.nombre = "Final"; e.nombreRival = "Goliat Ω"; e.esJefe = true;
                e.enemigos = new List<RobotData> { goliat };
            });

            ENEncuentro(enc1, "Round 1", "The Scrappers");
            ENEncuentro(enc2, "Semifinal", "Chrome Squad");
            ENEncuentro(enc3, "Final", "Goliath Ω");

            // ---------- Torneo ----------
            return Asset<TournamentData>($"{Datos}/Torneo.asset", t =>
            {
                t.nombre = "Torneo de Mechas";
                t.plantel = new List<RobotData> { bastion, vanguardia, nova, chispa };
                t.tamanoEquipo = 3;
                t.peleas = new List<EncounterData> { enc1, enc2, enc3 };
                t.velocidadBatalla = 1f;
                t.inventarioInicial = new List<ItemStack>
                {
                    new ItemStack { item = nucleoEnergia, cantidad = 4 },
                    new ItemStack { item = celda, cantidad = 3 },
                    new ItemStack { item = bomba, cantidad = 2 },
                    new ItemStack { item = pieza, cantidad = 2 },
                };
            });
        }

        // =====================================================================
        // Escena
        // =====================================================================
        static void CrearEscena()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Recargar desde disco después de NewScene (las referencias anteriores quedan inválidas).
            var torneo = AssetDatabase.LoadAssetAtPath<TournamentData>($"{Datos}/Torneo.asset");
            var vis = AssetDatabase.LoadAssetAtPath<BattleVisuals>($"{Raiz}/BattleVisuals.asset");
            if (torneo == null || vis == null)
                Debug.LogError("[TP4] No se encontraron Torneo.asset o BattleVisuals.asset.");

            ConstruirArena(vis);

            // Spawns: jugador a la derecha (+X), enemigos a la izquierda (−X), estilo FF7.
            var spawns = new GameObject("Spawns").transform;
            var sj = new Transform[3];
            var se = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                sj[i] = Spawn("Jugador " + (i + 1), spawns, new Vector3(5f + i * 0.4f, 0, -3.2f + i * 3.2f), Vector3.left);
                se[i] = Spawn("Enemigo " + (i + 1), spawns, new Vector3(-5f - i * 0.4f, 0, -3.2f + i * 3.2f), Vector3.right);
            }
            var sjefe = Spawn("Jefe", spawns, new Vector3(-6.5f, 0, 0), Vector3.right);

            // Cámara
            var battleCam = CrearCamara(new Vector3(10.5f, 6.2f, -8.5f), new Vector3(-0.5f, -0.6f, 0.4f), 42f);
            CrearPostProceso();

            // BattleManager
            var bmGO = new GameObject("BattleManager");
            var bm = bmGO.AddComponent<BattleManager>();
            var so = new SerializedObject(bm);
            so.FindProperty("torneo").objectReferenceValue = torneo;
            so.FindProperty("visuales").objectReferenceValue = vis;
            so.FindProperty("camara").objectReferenceValue = battleCam;
            so.FindProperty("spawnJefe").objectReferenceValue = sjefe;
            var pj = so.FindProperty("spawnsJugador");
            var pe = so.FindProperty("spawnsEnemigo");
            pj.arraySize = 3;
            pe.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                pj.GetArrayElementAtIndex(i).objectReferenceValue = sj[i];
                pe.GetArrayElementAtIndex(i).objectReferenceValue = se[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(escena, Escena);
        }

        static void CrearEscenaMenu()
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var torneo = AssetDatabase.LoadAssetAtPath<TournamentData>($"{Datos}/Torneo.asset");
            var vis = AssetDatabase.LoadAssetAtPath<BattleVisuals>($"{Raiz}/BattleVisuals.asset");

            ConstruirArena(vis);

            // Los 4 robots del plantel en fila, del lado derecho de la pantalla, mirando a cámara.
            var robots = new GameObject("Robots del menú").transform;
            var puntos = new Transform[4];
            var posCamara = new Vector3(-0.5f, 2.4f, -8.5f);
            for (int i = 0; i < 4; i++)
            {
                var pos = new Vector3(1.6f + i * 1.5f, 0, i % 2 == 0 ? -0.6f : 0.8f);
                var aCamara = posCamara - pos;
                aCamara.y = 0;
                puntos[i] = Spawn("Robot " + (i + 1), robots, pos, aCamara.normalized);
            }

            CrearCamara(posCamara, new Vector3(0.8f, 1.3f, 0f), 40f);
            CrearPostProceso();

            var go = new GameObject("MainMenu");
            var menu = go.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("torneo").objectReferenceValue = torneo;
            so.FindProperty("visuales").objectReferenceValue = vis;
            var pr = so.FindProperty("puntosRobots");
            pr.arraySize = puntos.Length;
            for (int i = 0; i < puntos.Length; i++) pr.GetArrayElementAtIndex(i).objectReferenceValue = puntos[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(escena, EscenaMenu);
        }

        /// <summary>Iluminación y arena (compartida por el combate y el menú).</summary>
        static void ConstruirArena(BattleVisuals vis)
        {
            // Iluminación
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.4f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.22f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.1f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 60f;

            var sol = new GameObject("Luz principal").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.intensity = 1.3f;
            sol.color = new Color(1f, 0.95f, 0.88f);
            sol.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.Euler(50f, -40f, 0f);

            // Arena
            var arena = new GameObject("Arena");
            var emisivoCian = Mat("MAT_NeonCian", "Universal Render Pipeline/Unlit", new Color(0.2f, 0.85f, 1f), true);
            var emisivoMagenta = Mat("MAT_NeonMagenta", "Universal Render Pipeline/Unlit", new Color(1f, 0.25f, 0.7f), true);
            var metal = Mat("MAT_Metal", "Universal Render Pipeline/Lit", new Color(0.22f, 0.24f, 0.3f));

            var piso = Primitiva(PrimitiveType.Cylinder, "Piso", arena.transform, new Vector3(0, -0.1f, 0), new Vector3(24, 0.1f, 24), vis != null ? vis.materialPiso : null);
            Primitiva(PrimitiveType.Cylinder, "Borde neón", arena.transform, new Vector3(0, -0.13f, 0), new Vector3(24.6f, 0.08f, 24.6f), emisivoCian);
            Primitiva(PrimitiveType.Cylinder, "Círculo central", arena.transform, new Vector3(0, -0.04f, 0), new Vector3(6f, 0.02f, 6f), metal);
            Primitiva(PrimitiveType.Cube, "Línea central", arena.transform, new Vector3(0, -0.03f, 0), new Vector3(0.15f, 0.02f, 22f), emisivoMagenta);

            for (int i = 0; i < 10; i++)
            {
                float ang = i * Mathf.PI * 2f / 10f;
                var pos = new Vector3(Mathf.Cos(ang) * 18f, 3f, Mathf.Sin(ang) * 18f);
                var pilar = Primitiva(PrimitiveType.Cube, "Pilar " + i, arena.transform, pos, new Vector3(1.2f, 6f, 1.2f), metal);
                pilar.transform.LookAt(new Vector3(0, 3f, 0));
                Primitiva(PrimitiveType.Cube, "Neón", pilar.transform, new Vector3(0, 0, 0.55f), new Vector3(0.3f, 0.9f, 0.05f), i % 2 == 0 ? emisivoCian : emisivoMagenta);

                if (i % 2 == 0)
                {
                    var luz = new GameObject("Luz pilar " + i).AddComponent<Light>();
                    luz.type = LightType.Point;
                    luz.range = 12f;
                    luz.intensity = 3f;
                    luz.color = i % 4 == 0 ? new Color(0.3f, 0.85f, 1f) : new Color(1f, 0.3f, 0.7f);
                    luz.transform.SetParent(arena.transform);
                    luz.transform.position = pos * 0.85f + Vector3.up * 1f;
                }
            }
        }

        static BattleCamera CrearCamara(Vector3 pos, Vector3 mirar, float fov)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            cam.fieldOfView = fov;
            cam.farClipPlane = 200f;
            camGO.AddComponent<AudioListener>();
            camGO.transform.position = pos;
            camGO.transform.LookAt(mirar);
            var battleCam = camGO.AddComponent<BattleCamera>();
            var datosCam = cam.GetUniversalAdditionalCameraData();
            if (datosCam != null) datosCam.renderPostProcessing = true;
            return battleCam;
        }

        static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material mat)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            go.transform.localPosition = pos;
            go.transform.localScale = escala;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        static Transform Spawn(string nombre, Transform padre, Vector3 pos, Vector3 mirar)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre);
            t.position = pos;
            t.rotation = Quaternion.LookRotation(mirar);
            return t;
        }

        static void CrearPostProceso()
        {
            string ruta = $"{Raiz}/PostProceso.asset";
            var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ruta);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, ruta);

                var bloom = perfil.Add<Bloom>(true);
                bloom.threshold.Override(0.9f);
                bloom.intensity.Override(1.4f);
                bloom.scatter.Override(0.7f);

                var vignette = perfil.Add<Vignette>(true);
                vignette.intensity.Override(0.32f);

                var tone = perfil.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.ACES);

                // Los componentes del perfil se guardan como sub-assets.
                foreach (var c in perfil.components) AssetDatabase.AddObjectToAsset(c, perfil);
                EditorUtility.SetDirty(perfil);
                AssetDatabase.SaveAssets();
            }

            var vol = new GameObject("Post Proceso").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = perfil;
        }

        /// <summary>Menú principal primero (es la escena con la que arranca el juego), luego el resto.</summary>
        static void ConfigurarBuild()
        {
            var escenas = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(EscenaMenu, true),
                new EditorBuildSettingsScene(Escena, true)
            };
            string seleccion = Raiz + "/Scenes/SeleccionEquipo.unity";
            if (File.Exists(seleccion)) escenas.Insert(1, new EditorBuildSettingsScene(seleccion, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
