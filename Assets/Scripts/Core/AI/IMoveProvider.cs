namespace Core.AI {
    public interface IMoveProvider {
        Move? GetNextMove(Board board);
    }
}