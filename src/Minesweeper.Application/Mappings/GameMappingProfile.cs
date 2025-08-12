using AutoMapper;
using Minesweeper.Application.DTOs;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.Entities;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Application.Mappings;

public class GameMappingProfile : Profile
{
    public GameMappingProfile()
    {
        CreateMap<Game, GameDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.PlayerId.Value))
            .ForMember(dest => dest.DifficultyName, opt => opt.MapFrom(src => src.Board.Difficulty.Name))
            .ForMember(dest => dest.Rows, opt => opt.MapFrom(src => src.Board.Difficulty.Rows))
            .ForMember(dest => dest.Columns, opt => opt.MapFrom(src => src.Board.Difficulty.Columns))
            .ForMember(dest => dest.MineCount, opt => opt.MapFrom(src => src.Board.Difficulty.MineCount))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.GetGameDuration()))
            .ForMember(dest => dest.RemainingMines, opt => opt.MapFrom(src => src.GetRemainingMineCount()))
            .ForMember(dest => dest.ProgressPercentage, opt => opt.MapFrom(src => src.GetProgressPercentage()))
            .ForMember(dest => dest.Board, opt => opt.MapFrom(src => src.Board));

        CreateMap<GameBoard, BoardDto>()
            .ForMember(dest => dest.Rows, opt => opt.MapFrom(src => src.Difficulty.Rows))
            .ForMember(dest => dest.Columns, opt => opt.MapFrom(src => src.Difficulty.Columns))
            .ForMember(dest => dest.Cells, opt => opt.MapFrom(src => CreateCellArray(src)));

        CreateMap<Cell, CellDto>()
            .ForMember(dest => dest.Row, opt => opt.MapFrom(src => src.Position.Row))
            .ForMember(dest => dest.Column, opt => opt.MapFrom(src => src.Position.Column))
            .ForMember(dest => dest.DisplayValue, opt => opt.MapFrom(src => src.GetDisplayValue()));

        CreateMap<GameStatistics, GameStatisticsDto>()
            .ForMember(dest => dest.GameId, opt => opt.MapFrom(src => src.GameId.Value))
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.PlayerId.Value))
            .ForMember(dest => dest.DifficultyName, opt => opt.MapFrom(src => src.Difficulty.Name));

        CreateMap<Domain.Aggregates.PlayerStatistics, PlayerStatisticsDto>();

        CreateMap<Domain.Repositories.PlayerLeaderboardEntry, PlayerStatisticsDto>()
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.PlayerId.Value));

        CreateMap<GameDifficulty, GameDifficultyDto>();
    }

    private static CellDto[][] CreateCellArray(GameBoard board)
    {
        var cells = new CellDto[board.Difficulty.Rows][];

        for (int row = 0; row < board.Difficulty.Rows; row++)
        {
            cells[row] = new CellDto[board.Difficulty.Columns];
            for (int col = 0; col < board.Difficulty.Columns; col++)
            {
                var position = CellPosition.Of(row, col);
                var cell = board.GetCell(position);

                cells[row][col] = new CellDto
                {
                    Row = row,
                    Column = col,
                    State = cell.State,
                    HasMine = cell.HasMine,
                    AdjacentMineCount = cell.AdjacentMineCount,
                    DisplayValue = cell.GetDisplayValue(),
                    IsRevealed = cell.IsRevealed,
                    IsFlagged = cell.IsFlagged,
                    IsHidden = cell.IsHidden
                };
            }
        }

        return cells;
    }
}
