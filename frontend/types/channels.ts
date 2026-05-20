export enum ChannelType {
  Email = 0,
  Phone = 1,
  Chat = 2,
  WhatsApp = 3,
  Social = 4,
}

export interface ChannelMessage {
  id: string;
  type: ChannelType;
  sender: string;
  recipient: string;
  content: string;
  sentAtUtc: string;
  externalId?: string;
  metadata?: Record<string, string>;
}
