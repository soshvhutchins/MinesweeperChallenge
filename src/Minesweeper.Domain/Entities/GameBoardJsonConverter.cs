using System.Text.Json;
using System.Text.Json.Serialization;
namespace Minesweeper.Domain.Entities;

public static class GameBoardJsonConverter
{
    public static string Serialize(GameBoard board)
    {
        return JsonSerializer.Serialize(board, new JsonSerializerOptions
        {
            WriteIndented = false,
            IncludeFields = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        });
    }

    public static GameBoard Deserialize(string json)
    {
        return JsonSerializer.Deserialize<GameBoard>(json, new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        })!;
    }
}
