export interface SquadModalPlayer {
  playerId: string;
  name: string;
  position: string | null;
  overall: number | null;
  avatarUrl: string | null;

  slotIndex?: number;

  x?: number;
  y?: number;
}

export interface FormationSlot {
  position: string;
  left: number;
  top: number;
}

export interface SaveLineupPayload {
  formation: string;
  players: {
    playerId: string;
    slotIndex: number;
    position: string;
  }[];
}