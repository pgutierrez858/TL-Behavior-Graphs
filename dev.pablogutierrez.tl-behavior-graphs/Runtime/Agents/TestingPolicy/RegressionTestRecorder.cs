using System.IO.Abstractions;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;
using Unity.MLAgents.Policies;
using UnityEngine.Serialization;
using Unity.MLAgents;
using System;

namespace TLTLUnity.Agents.Tests
{
    /// <summary>
    /// Component inspired by ML-Agents DemonstrationRecorder to record the results of regression tests via RegressionTestWriters.
    /// </remarks>
    [RequireComponent(typeof(BaseMLCharacterController))]
    [AddComponentMenu("Regression Testing/Test Recorder")]
    public class RegressionTestRecorder : MonoBehaviour
    {
        /// <summary>
        /// Whether or not to record test data.
        /// </summary>
        [FormerlySerializedAs("record")]
        [Tooltip("Whether or not to record test data.")]
        public bool Record;

        /// <summary>
        /// Number of episodes to record. The editor will stop playing when it reaches this threshold.
        /// Set to zero to record indefinitely.
        /// </summary>
        [Tooltip("Number of episodes to record. The editor will stop playing when it reaches this threshold. " +
            "Set to zero to record indefinitely.")]
        public int NumEpisodesToRecord;

        /// <summary>
        /// Base test file name. If multiple files are saved, the additional filenames
        /// will have a sequence of unique numbers appended.
        /// </summary>
        [FormerlySerializedAs("regressionTestName")]
        [Tooltip("Base test file name. If multiple files are saved, the additional " +
            "filenames will have a unique number appended.")]
        public string RegressionTestName;

        /// <summary>
        /// Directory to save the test files. Will default to a "Tests/" folder in the
        /// Application data path if not specified.
        /// </summary>
        [FormerlySerializedAs("regressionTestDirectory")]
        [Tooltip("Directory to save the regression test files. Will default to " +
            "{Application.dataPath}/RegressionTests if not specified.")]
        public string RegressionTestDirectory;

        RegressionTestWriter m_EpisodeTestWriter;
        RegressionTestWriter m_StepTestWriter;

        const string k_ExtensionType = ".csv";
        const string k_DefaultDirectoryName = "Tests";
        IFileSystem m_FileSystem;

        BaseMLCharacterController m_Agent;

        internal enum TestWriterType
        {
            Episode, Step
        }

        void OnEnable()
        {
            m_Agent = GetComponent<BaseMLCharacterController>();
        }

        void Update()
        {
            if (!Record)
            {
                return;
            }

            LazyInitialize(TestWriterType.Episode);
            LazyInitialize(TestWriterType.Step);

            // Quit when num steps to record is reached
            Debug.Log($"[RegressionTestRecorderComponent] {m_EpisodeTestWriter.WriteCount} Episodes Written.");
            if (NumEpisodesToRecord > 0 && m_EpisodeTestWriter.WriteCount >= NumEpisodesToRecord)
            {
                Application.Quit(0);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }

        /// <summary>
        /// Creates regression test store for use in recording.
        /// Has no effect if the regression test store was already created.
        /// </summary>
        internal void LazyInitialize(TestWriterType writerType, IFileSystem fileSystem = null)
        {
            RegressionTestWriter regressionTestWriter = writerType == TestWriterType.Episode ? m_EpisodeTestWriter : m_StepTestWriter;
            string testNamePreffix = Enum.GetName(typeof(TestWriterType), writerType);

            if (regressionTestWriter != null)
            {
                return;
            }

            if (m_Agent == null)
            {
                m_Agent = GetComponent<BaseMLCharacterController>();
            }

            m_FileSystem = fileSystem ?? new FileSystem();
            var behaviorParams = GetComponent<BehaviorParameters>();

            string testName = RegressionTestName;
            if (string.IsNullOrEmpty(testName))
            {
                testName = behaviorParams.BehaviorName;
            }
            if (string.IsNullOrEmpty(RegressionTestDirectory))
            {
                RegressionTestDirectory = Path.Combine(Application.dataPath, k_DefaultDirectoryName);
            }

            testName = $"{SanitizeName(testName)}_{testNamePreffix}";
            var filePath = MakeRegressionTestFilePath(m_FileSystem, RegressionTestDirectory, testName);
            var stream = m_FileSystem.File.Create(filePath);

            var writer = new RegressionTestWriter(stream);
            if (writerType == TestWriterType.Episode)
            {
                m_EpisodeTestWriter = writer;
                m_Agent.AddEpisodeTestWriter(m_EpisodeTestWriter);
            }
            else
            {
                // TODO: Manage step writers
                m_StepTestWriter = writer;
                //m_Agent.AddStepTestWriter(m_StepTestWriter);
            }

        }

        /// <summary>
        /// Removes all characters except alphanumerics from regression test name.
        /// </summary>
        internal static string SanitizeName(string regressionTestName)
        {
            var rgx = new Regex("[^a-zA-Z0-9 -]");
            regressionTestName = rgx.Replace(regressionTestName, "");
            return regressionTestName;
        }

        /// <summary>
        /// Gets a unique path for the RegressionTestName in the RegressionTestDirectory.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="regressionTestDirectory"></param>
        /// <param name="regressionTestName"></param>
        /// <returns></returns>
        internal static string MakeRegressionTestFilePath(
            IFileSystem fileSystem, string regressionTestDirectory, string regressionTestName
        )
        {
            // Create the directory if it doesn't already exist
            if (!fileSystem.Directory.Exists(regressionTestDirectory))
            {
                fileSystem.Directory.CreateDirectory(regressionTestDirectory);
            }

            var literalName = regressionTestName;
            var filePath = Path.Combine(regressionTestDirectory, literalName + k_ExtensionType);
            var uniqueNameCounter = 0;
            while (fileSystem.File.Exists(filePath))
            {
                literalName = regressionTestName + "_" + uniqueNameCounter;
                filePath = Path.Combine(regressionTestDirectory, literalName + k_ExtensionType);
                uniqueNameCounter++;
            }

            return filePath;
        }

        /// <summary>
        /// Close the RegressionTestWriters.
        /// Has no effect if the RegressionTestWriters are already closed (or weren't opened)
        /// </summary>
        public void Close()
        {
            if (m_EpisodeTestWriter != null)
            {
                m_EpisodeTestWriter.Close();
                m_EpisodeTestWriter = null;
            }

            if (m_StepTestWriter != null)
            {
                m_StepTestWriter.Close();
                m_StepTestWriter = null;
            }
        }

        /// <summary>
        /// Clean up the RegressionTestWriters when shutting down or destroying the Agent.
        /// </summary>
        void OnDestroy()
        {
            Close();
        }
    }
}
