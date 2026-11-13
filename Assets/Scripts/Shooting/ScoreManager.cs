using TMPro;
using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Singleton quản lý điểm bắn bia; cập nhật TextMeshPro UI mỗi khi điểm thay đổi.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        static ScoreManager instance;

        /// <summary>Singleton — ShootingTarget và code khác dùng ScoreManager.Instance?.AddScore(n).</summary>
        public static ScoreManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<ScoreManager>();
                return instance;
            }
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("UI")]
        [SerializeField, Tooltip("TextMeshPro hiện điểm (World Space Canvas hoặc Screen Space). Rỗng = không hiển thị điểm.")]
        TMP_Text scoreText;

        [SerializeField, Tooltip("Format chuỗi hiện điểm. {0} = điểm. Ví dụ: 'SCORE: {0}' hoặc '{0} pts'.")]
        string scoreFormat = "SCORE: {0}";

        [Header("Reset")]
        [SerializeField, Tooltip("Reset điểm về 0 khi Scene Load.")]
        bool resetOnStart = true;

        // ── State ─────────────────────────────────────────────────────────────────
        int currentScore;

        /// <summary>Điểm hiện tại (read-only từ ngoài).</summary>
        public int CurrentScore => currentScore;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Awake()
        {
            // Singleton guard: chỉ giữ một instance
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        void Start()
        {
            if (resetOnStart)
                ResetScore();
            else
                RefreshUI();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Cộng điểm và cập nhật UI ngay.</summary>
        public void AddScore(int value)
        {
            currentScore += value;
            RefreshUI();
        }

        /// <summary>Đặt lại điểm về 0 và cập nhật UI.</summary>
        public void ResetScore()
        {
            currentScore = 0;
            RefreshUI();
        }

        // ── Private ───────────────────────────────────────────────────────────────

        /// <summary>Ghi text theo scoreFormat lên TMP_Text đã gán.</summary>
        void RefreshUI()
        {
            if (scoreText == null) return;
            scoreText.text = string.Format(scoreFormat, currentScore);
        }
    }
}
