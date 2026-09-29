using UnityEngine;

namespace CatHotel.Services
{
    [CreateAssetMenu(fileName = "AuthConfig", menuName = "Meowtel/Auth Config")]
    public class AuthConfig : ScriptableObject
    {
        [Header("Google Play Games")]
        [Tooltip("OAuth Web Client ID depuis Google Cloud Console")]
        public string webClientId;

        [Header("UGS")]
        [Tooltip("Environnement UGS de l'éditeur et des Development Builds (données de test).")]
        public string environmentName = DevEnvironment;

        [Tooltip("Environnement UGS des builds non-Development (Play Store). Les sauvegardes cloud des joueurs " +
                 "<= 0.40 sont dans 'development' : ne passer à 'production' qu'après les y avoir copiées.")]
        public string releaseEnvironmentName = ReleaseEnvironment;

        public const string DevEnvironment = "development";
        public const string ReleaseEnvironment = "production";

        // static readonly rather than const, so the ternaries below don't raise unreachable-code warnings
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static readonly bool IsDevBuild = true;
#else
        public static readonly bool IsDevBuild = false;
#endif

        public static string BuildKind =>
            Application.isEditor ? "editor" : IsDevBuild ? "development build" : "release build";

        /// <summary>
        /// UGS environment for this build: the dev one in the Editor and in Development Builds,
        /// the release one otherwise. Never empty (SetEnvironmentName throws on an empty name).
        /// </summary>
        public static string ResolveEnvironmentName(AuthConfig config)
        {
            string env = config == null ? null
                : IsDevBuild ? config.environmentName : config.releaseEnvironmentName;
            if (string.IsNullOrWhiteSpace(env))
                return IsDevBuild ? DevEnvironment : ReleaseEnvironment;
            return env.Trim();
        }
    }
}
