export interface FormationPlayerPosition {
  position: string;
  top: number;
  left: number;
}

export const FORMATIONS = [
  '4-3-3 Control',
  '4-2-3-1 Wide',
  '4-4-2 Flat',
  '4-1-2-1-2 Narrow',
  '3-5-2',
  '5-3-2',
  '4-5-1'
];

export const FORMATION_LAYOUTS: Record<string, FormationPlayerPosition[]> = {
  '4-3-3 Control': [
    { position: 'GK', top: 88, left: 50 },
    { position: 'LB', top: 70, left: 18 },
    { position: 'CB', top: 72, left: 38 },
    { position: 'CB', top: 72, left: 62 },
    { position: 'RB', top: 70, left: 82 },
    { position: 'CM', top: 50, left: 30 },
    { position: 'CM', top: 46, left: 50 },
    { position: 'CM', top: 50, left: 70 },
    { position: 'LW', top: 24, left: 22 },
    { position: 'ST', top: 18, left: 50 },
    { position: 'RW', top: 24, left: 78 }
  ],

  '4-2-3-1 Wide': [
    { position: 'GK', top: 88, left: 50 },
    { position: 'LB', top: 70, left: 18 },
    { position: 'CB', top: 72, left: 38 },
    { position: 'CB', top: 72, left: 62 },
    { position: 'RB', top: 70, left: 82 },
    { position: 'CDM', top: 56, left: 38 },
    { position: 'CDM', top: 56, left: 62 },
    { position: 'LM', top: 38, left: 22 },
    { position: 'CAM', top: 34, left: 50 },
    { position: 'RM', top: 38, left: 78 },
    { position: 'ST', top: 18, left: 50 }
  ],

  '4-4-2 Flat': [
    { position: 'GK', top: 88, left: 50 },
    { position: 'LB', top: 70, left: 18 },
    { position: 'CB', top: 72, left: 38 },
    { position: 'CB', top: 72, left: 62 },
    { position: 'RB', top: 70, left: 82 },
    { position: 'LM', top: 48, left: 18 },
    { position: 'CM', top: 50, left: 40 },
    { position: 'CM', top: 50, left: 60 },
    { position: 'RM', top: 48, left: 82 },
    { position: 'ST', top: 22, left: 42 },
    { position: 'ST', top: 22, left: 58 }
  ],

  '4-1-2-1-2 Narrow': [
    { position: 'GK', top: 88, left: 50 },
    { position: 'LB', top: 70, left: 18 },
    { position: 'CB', top: 72, left: 38 },
    { position: 'CB', top: 72, left: 62 },
    { position: 'RB', top: 70, left: 82 },
    { position: 'CDM', top: 58, left: 50 },
    { position: 'CM', top: 46, left: 38 },
    { position: 'CM', top: 46, left: 62 },
    { position: 'CAM', top: 34, left: 50 },
    { position: 'ST', top: 20, left: 42 },
    { position: 'ST', top: 20, left: 58 }
  ],

  '3-5-2': [
    { position: 'GK', top: 88, left: 50 },
    { position: 'CB', top: 72, left: 30 },
    { position: 'CB', top: 74, left: 50 },
    { position: 'CB', top: 72, left: 70 },
    { position: 'LM', top: 52, left: 16 },
    { position: 'CM', top: 50, left: 36 },
    { position: 'CDM', top: 56, left: 50 },
    { position: 'CM', top: 50, left: 64 },
    { position: 'RM', top: 52, left: 84 },
    { position: 'ST', top: 22, left: 42 },
    { position: 'ST', top: 22, left: 58 }
  ],

  '5-3-2': [
    { position: 'GK', top: 88, left: 50 },
    { position: 'LWB', top: 68, left: 12 },
    { position: 'CB', top: 74, left: 32 },
    { position: 'CB', top: 76, left: 50 },
    { position: 'CB', top: 74, left: 68 },
    { position: 'RWB', top: 68, left: 88 },
    { position: 'CM', top: 48, left: 35 },
    { position: 'CM', top: 44, left: 50 },
    { position: 'CM', top: 48, left: 65 },
    { position: 'ST', top: 22, left: 42 },
    { position: 'ST', top: 22, left: 58 }
  ],

  '4-5-1': [
    { position: 'GK', top: 88, left: 50 },
    { position: 'LB', top: 70, left: 18 },
    { position: 'CB', top: 72, left: 38 },
    { position: 'CB', top: 72, left: 62 },
    { position: 'RB', top: 70, left: 82 },
    { position: 'LM', top: 48, left: 18 },
    { position: 'CM', top: 50, left: 35 },
    { position: 'CM', top: 46, left: 50 },
    { position: 'CM', top: 50, left: 65 },
    { position: 'RM', top: 48, left: 82 },
    { position: 'ST', top: 20, left: 50 }
  ]
};