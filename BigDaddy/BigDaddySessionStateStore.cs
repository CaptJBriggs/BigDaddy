using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace BigDaddy
{
    // Stores the active countdown separately from the user's long-lived settings.
    internal class BigDaddySessionStateStore
    {
        private readonly string filePath =
            Path.Combine(Application.LocalUserAppDataPath, "session.json");

        public bool TryLoad(out BigDaddySessionState sessionState)
        {
            sessionState = new BigDaddySessionState();

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                BigDaddySessionState? loadedState = JsonSerializer.Deserialize<BigDaddySessionState>(
                    File.ReadAllText(filePath));

                if (loadedState == null || loadedState.RemainingTime < TimeSpan.Zero)
                {
                    return false;
                }

                sessionState = loadedState;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public bool TrySave(BigDaddySessionState sessionState)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                File.WriteAllText(filePath, JsonSerializer.Serialize(sessionState));
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        // Removes the recoverable schedule state after the user deliberately stops it.
        public bool TryClear()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
