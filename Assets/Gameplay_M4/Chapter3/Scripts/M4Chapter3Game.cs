using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PRU.M4
{
    [DisallowMultipleComponent]
    public sealed class M4Chapter3Game : MonoBehaviour
    {
        public enum RoundState { Playing, Won, Lost }
        public M4Chapter3Player player;
        public M4Chapter3Boss boss;
        public M4Chapter3Health[] requiredEnemies = Array.Empty<M4Chapter3Health>();
        public bool requireSmallEnemies;
        public bool showTemporaryHud = true, useBuiltInInput = true;
        public Font hudFont;
        public string sceneLabel = "CHƯƠNG 3 · MIẾU CHẰN TINH";
        public RoundState State { get; private set; } = RoundState.Playing;
        public bool IsPaused { get; private set; }
        public bool IsRoundActive => isActiveAndEnabled && roundStarted && !IsPaused && State == RoundState.Playing;
        public event Action<M4Chapter3Game> Changed;
        public event Action<RoundState> RoundEnded;
        public event Action<M4Chapter3Game> RoundStarted;
        public event Action<bool> PausedChanged;
        private readonly HashSet<M4Chapter3Health> subscribedHealth = new HashSet<M4Chapter3Health>();
        private readonly Dictionary<M4Chapter3Health, Pose> enemySpawns = new Dictionary<M4Chapter3Health, Pose>();
        private bool roundStarted, resetting;
        private GUIStyle titleStyle, bodyStyle, smallStyle, centerStyle, buttonStyle;
        private static readonly Color Panel = new Color(.045f, .09f, .09f, .94f), Gold = new Color(.98f, .81f, .42f);

        private void Start() => BeginRound();
        private void OnEnable() { BindHealth(); if (roundStarted) ApplyRoundLocks(); }
        private void Update()
        {
            if (useBuiltInInput && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) RetryRound();
        }
        private void BindHealth()
        {
            UnbindHealth();
            Bind(player != null ? player.health : null); Bind(boss != null ? boss.health : null);
            if (requiredEnemies == null) return;
            foreach (var enemy in requiredEnemies)
            {
                Bind(enemy);
                if (enemy != null && !enemySpawns.ContainsKey(enemy))
                    enemySpawns.Add(enemy, new Pose(enemy.transform.position, enemy.transform.rotation));
            }
        }
        private void Bind(M4Chapter3Health health)
        {
            if (health == null || !subscribedHealth.Add(health)) return;
            health.Changed += OnHealthChanged;
        }
        private void UnbindHealth()
        {
            foreach (var health in subscribedHealth) if (health != null) health.Changed -= OnHealthChanged;
            subscribedHealth.Clear();
        }
        private void OnHealthChanged(M4Chapter3Health health)
        {
            if (resetting) return;
            RoundState before = State;
            CheckOutcome();
            if (before == State) Changed?.Invoke(this);
        }
        public void BeginRound()
        {
            if (!isActiveAndEnabled || player == null || boss == null || player.health == null || boss.health == null) return;
            resetting = true;
            BindHealth();
            player.game = this; boss.game = this; boss.player = player;
            bool wasPaused = IsPaused;
            State = RoundState.Playing; roundStarted = true; IsPaused = false;
            player.ResetCombat(); boss.ResetCombat();
            if (requiredEnemies != null)
                foreach (var enemy in requiredEnemies)
                {
                    if (enemy == null || enemy == player.health || enemy == boss.health) continue;
                    if (enemySpawns.TryGetValue(enemy, out Pose spawn)) enemy.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                    enemy.ResetHealth();
                }
            Physics.SyncTransforms();
            resetting = false;
            ApplyRoundLocks();
            if (wasPaused) PausedChanged?.Invoke(false);
            Changed?.Invoke(this); RoundStarted?.Invoke(this);
        }
        public void RetryRound() => BeginRound();
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            ApplyRoundLocks();
            PausedChanged?.Invoke(paused); Changed?.Invoke(this);
        }
        private void ApplyRoundLocks()
        {
            bool active = IsRoundActive;
            if (player != null) player.SetInputLocked(!active);
            if (boss != null) boss.SetCombatPaused(!active);
            foreach (var health in subscribedHealth) if (health != null) health.SetDamageEnabled(active);
        }
        public void CheckOutcome()
        {
            if (resetting || !IsRoundActive || player == null || boss == null || player.health == null || boss.health == null) return;
            if (!player.health.IsAlive) EndRound(RoundState.Lost);
            else if (!boss.health.IsAlive && AllRequiredEnemiesDefeated()) EndRound(RoundState.Won);
        }
        private bool AllRequiredEnemiesDefeated()
        {
            if (!requireSmallEnemies) return true;
            if (requiredEnemies == null || requiredEnemies.Length == 0) return false;
            foreach (var enemy in requiredEnemies) if (enemy == null || enemy.IsAlive) return false;
            return true;
        }
        private void EndRound(RoundState result)
        {
            if (!IsRoundActive) return;
            State = result;
            ApplyRoundLocks();
            Changed?.Invoke(this); RoundEnded?.Invoke(result);
        }
        private void OnDisable()
        {
            ApplyRoundLocks(); UnbindHealth();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && titleStyle.font == hudFont) return;
            titleStyle = new GUIStyle(GUI.skin.label) { font = hudFont, fontSize = 20, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = Gold;
            bodyStyle = new GUIStyle(GUI.skin.label) { font = hudFont, fontSize = 17, wordWrap = true };
            bodyStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(bodyStyle) { fontSize = 14 };
            centerStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 23, fontStyle = FontStyle.Bold };
            buttonStyle = new GUIStyle(GUI.skin.button) { font = hudFont, fontSize = 18, fontStyle = FontStyle.Bold };
        }
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private void HealthBar(Rect rect, string name, M4Chapter3Health health, Color color)
        {
            if (health == null) return;
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 24), $"{name}   {Mathf.CeilToInt(health.CurrentHealth)} / {Mathf.CeilToInt(health.maxHealth)}", bodyStyle);
            var bar = new Rect(rect.x, rect.y + 27, rect.width, 12);
            Fill(bar, new Color(.12f, .17f, .17f));
            bar.width *= Mathf.Clamp01(health.CurrentHealth / Mathf.Max(1, health.maxHealth)); Fill(bar, color);
        }
        private string BossStatus()
        {
            if (IsPaused && State == RoundState.Playing) return "TRẬN ĐẤU ĐANG TẠM DỪNG.";
            if (boss == null) return "Chưa gắn Chằn Tinh vào màn chơi.";
            if (!string.IsNullOrEmpty(boss.NavigationFailureReason)) return boss.NavigationFailureReason;
            switch (boss.State)
            {
                case M4Chapter3Boss.BossState.Telegraph: return $"SẮP ĐÁNH · {boss.StateTimeRemaining:0.0}s — né sang bên!";
                case M4Chapter3Boss.BossState.Strike: return "CHẰN TINH ĐANG ĐÁNH — giữ khoảng cách!";
                case M4Chapter3Boss.BossState.Recovery: return $"ĐANG HỒI SỨC · {boss.StateTimeRemaining:0.0}s — cơ hội phản công.";
                case M4Chapter3Boss.BossState.Chase: return "Chằn Tinh đang đuổi theo bạn.";
                case M4Chapter3Boss.BossState.Dead: return requireSmallEnemies ? "Chằn Tinh đã bị hạ. Hoàn tất các mục tiêu còn lại." : "Chằn Tinh đã bị hạ.";
                default: return "Tiến lại gần Chằn Tinh để bắt đầu trận đấu.";
            }
        }
        private void OnGUI()
        {
            if (!showTemporaryHud) return;
            EnsureStyles();
            const float designWidth = 1280, designHeight = 720;
            float scale = Mathf.Min(Screen.width / designWidth, Screen.height / designHeight);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - designWidth * scale) * .5f, (Screen.height - designHeight * scale) * .5f), Quaternion.identity, Vector3.one * scale);
            Fill(new Rect(20, 20, 475, 175), Panel);
            GUI.Label(new Rect(38, 32, 445, 29), sceneLabel, titleStyle);
            HealthBar(new Rect(38, 76, 205, 42), "THẠCH SANH", player != null ? player.health : null, new Color(.3f, .8f, .55f));
            HealthBar(new Rect(269, 76, 205, 42), "CHẰN TINH", boss != null ? boss.health : null, new Color(.9f, .36f, .25f));
            GUI.Label(new Rect(38, 138, 440, 44), requireSmallEnemies ? "Mục tiêu: hạ Chằn Tinh và các quái đã được giao." : "Mục tiêu: hạ Chằn Tinh bằng rìu; né đòn để giữ máu.", smallStyle);
            Fill(new Rect(20, 618, 1240, 82), Panel);
            GUI.Label(new Rect(38, 629, 1205, 27), BossStatus(), bodyStyle);
            GUI.Label(new Rect(38, 663, 1205, 24), "WASD: di chuyển   Shift: chạy   Space: nhảy   Giữ chuột phải: xoay camera   Chuột trái: vung rìu   R: chơi lại từ đầu", smallStyle);
            if (roundStarted && (State != RoundState.Playing || IsPaused))
            {
                Fill(new Rect(405, 235, 470, 225), Panel);
                bool paused = State == RoundState.Playing;
                GUI.Label(new Rect(425, 253, 430, 58), paused ? "TẠM DỪNG" : State == RoundState.Won ? "CHIẾN THẮNG!" : "THẠCH SANH ĐÃ GỤC NGÃ", centerStyle);
                GUI.Label(new Rect(433, 315, 414, 62), paused ? "Trận đấu sẽ tiếp tục khi bạn sẵn sàng." : State == RoundState.Won ? "Bạn đã hoàn tất trận đánh. Có thể chơi lại để luyện né đòn." : "Hãy né lúc Chằn Tinh báo đòn, rồi phản công khi hắn hồi sức.", bodyStyle);
                if (GUI.Button(new Rect(505, 398, 270, 43), paused ? "TIẾP TỤC" : "CHƠI LẠI [R]", buttonStyle))
                {
                    if (paused) SetPaused(false); else RetryRound();
                }
            }
            GUI.matrix = previousMatrix;
        }
    }
}
