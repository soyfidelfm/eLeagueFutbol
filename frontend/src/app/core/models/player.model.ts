export interface Player {
  rank: number;
  name: string;
  teamName?: string;
  teamLogo?: string;
  isMe?: boolean;
  points: number;
  gd: string;
  record: string;
  avatar: string;
  form: string[];
}