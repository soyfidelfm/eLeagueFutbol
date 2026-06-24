import { DraftOrder } from "./draft.model";

export interface DraftLotteryPreview {
  drawNumber: number;
  isFinal: boolean;
  draftOrder: DraftOrder[];
}