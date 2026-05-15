export const queryKeys = {
  dashboard: {
    overview: (days: number) => ["dashboard", "overview", { days }] as const
  },
  pipeline: {
    board: ["pipeline", "board"] as const
  },
  rules: {
    list: ["rules", "list"] as const
  },
  email: {
    logs: (page: number, pageSize: number, search: string) =>
      ["email", "logs", { page, pageSize, search }] as const
  }
};
