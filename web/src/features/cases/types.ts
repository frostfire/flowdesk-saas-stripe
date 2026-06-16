export type CaseSummary = {
  id: string;
  reference: string;
  title: string;
  customerName: string;
  status: string;
  createdAt: string;
  updatedAt: string;
};

export type CaseDetail = CaseSummary & {
  description: string;
};

export type CaseMutationValues = {
  title: string;
  description: string;
  customerName: string;
};
