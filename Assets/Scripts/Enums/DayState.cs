namespace Enums
{
    public enum EDayState
    {
        None = -1       // not set
        , Start = 0     // beginning of a day (instructions and lore) 
        , Work = 1      // work cycle (orders and lore)
        , End = 2       // day summary (reputation check and lore)
        , Complete = 3  // fade in (day completion)
    }
}