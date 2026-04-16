Block<TState> {
  Operation<TState>[] Operations;
  Apply(TState state);
  Undo(TState state);
}

Operation<TState> {
  Apply(TState state);
  Undo(TState state);
}

IBlockchainState {
  IDispose EnterUpdateScope();

}

Blockchain<TBlock, TState>
 - TState State;
 - ApplyBlock(TBlock block) -> transitions State
 - Undo() -> transitions back to previous state
 
// Merklized Object Space Blockchain
 MOS : MerklizedObjectSpace, IBlockchainState {
 } 
 
 MOSB : Blockchain<MOSBlock, MOS>
   ApplyBlock 
      - 



