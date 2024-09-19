
using GameStateManagement.Core;
using System.Windows.Forms;

Application.SetHighDpiMode(HighDpiMode.SystemAware);  // Adjust the mode as needed
using var game = new GameStateManagementGame();
game.Run();
