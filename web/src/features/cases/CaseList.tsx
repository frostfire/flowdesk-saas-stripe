import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { useState } from "react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { getCurrentEntitlements } from "@/features/billing/billingApi";
import { approveCase, createCase, deleteCase, getCase, listCases, rejectCase, updateCase } from "./caseQueries";
import type { CaseMutationValues, CaseSummary } from "./types";

type CaseListProps = {
  token: string;
  onUnauthorized(): void;
};

export function CaseList({ token, onUnauthorized }: CaseListProps) {
  const queryClient = useQueryClient();
  const [editingCaseId, setEditingCaseId] = useState<string | null>(null);
  const query = useQuery({
    queryKey: ["cases"],
    queryFn: () => listCases(token, onUnauthorized),
  });
  const entitlements = useQuery({
    queryKey: ["entitlements"],
    queryFn: () => getCurrentEntitlements(token, onUnauthorized),
  });
  const editDetail = useQuery({
    queryKey: ["cases", editingCaseId],
    queryFn: () => getCase(token, editingCaseId ?? "", onUnauthorized),
    enabled: Boolean(editingCaseId),
  });
  const canMutate = entitlements.data?.entitlements.canCreateCases === true;
  const invalidateCases = () => {
    void queryClient.invalidateQueries({ queryKey: ["cases"] });
  };
  const create = useMutation({
    mutationFn: (values: CaseMutationValues) => createCase(token, values, onUnauthorized),
    onSuccess: invalidateCases,
  });
  const update = useMutation({
    mutationFn: (values: CaseMutationValues) => updateCase(token, editingCaseId ?? "", values, onUnauthorized),
    onSuccess: () => {
      setEditingCaseId(null);
      invalidateCases();
    },
  });
  const remove = useMutation({
    mutationFn: (id: string) => deleteCase(token, id, onUnauthorized),
    onSuccess: invalidateCases,
  });
  const approve = useMutation({
    mutationFn: (id: string) => approveCase(token, id, onUnauthorized),
    onSuccess: invalidateCases,
  });
  const reject = useMutation({
    mutationFn: (id: string) => rejectCase(token, id, onUnauthorized),
    onSuccess: invalidateCases,
  });

  if (query.isLoading) {
    return <CaseListLoading />;
  }

  if (query.isError) {
    return <CaseListError onRetry={() => void query.refetch()} />;
  }

  if (!query.data?.length) {
    return (
      <CasePanel canMutate={canMutate} createPending={create.isPending} onCreate={(values) => create.mutate(values)}>
        <CaseListEmpty />
      </CasePanel>
    );
  }

  return (
    <CasePanel canMutate={canMutate} createPending={create.isPending} onCreate={(values) => create.mutate(values)}>
      <CaseListData
        actionPending={remove.isPending || approve.isPending || reject.isPending}
        canMutate={canMutate}
        cases={query.data}
        editingCaseId={editingCaseId}
        editValues={
          editDetail.data
            ? {
                title: editDetail.data.title,
                description: editDetail.data.description,
                customerName: editDetail.data.customerName,
              }
            : null
        }
        updatePending={update.isPending}
        onApprove={(id) => approve.mutate(id)}
        onCancelEdit={() => setEditingCaseId(null)}
        onDelete={(id) => remove.mutate(id)}
        onEdit={(id) => setEditingCaseId(id)}
        onReject={(id) => reject.mutate(id)}
        onUpdate={(values) => update.mutate(values)}
      />
    </CasePanel>
  );
}

function CaseListLoading() {
  return (
    <div className="grid gap-3" aria-label="Loading cases">
      {["one", "two", "three"].map((item) => (
        <div key={item} className="h-20 animate-pulse rounded-md border border-border bg-slate-100" />
      ))}
    </div>
  );
}

function CaseListEmpty() {
  return (
    <div className="rounded-md border border-dashed border-border bg-white p-6">
      <h2 className="text-base font-semibold">No cases</h2>
      <p className="mt-2 text-sm text-slate-600">CaseFlow has no cases ready for this workspace.</p>
    </div>
  );
}

function CasePanel({
  canMutate,
  children,
  createPending,
  onCreate,
}: {
  canMutate: boolean;
  children: ReactNode;
  createPending: boolean;
  onCreate(values: CaseMutationValues): void;
}) {
  return (
    <section className="grid gap-4">
      <div className="rounded-md border border-blue-200 bg-blue-50 p-4 text-sm text-blue-950">
        Sandbox data — the cases here are yours alone and kept in memory for the demo. Your changes don't affect anyone
        else and reset when the demo restarts.
      </div>
      <CaseForm
        disabled={!canMutate || createPending}
        initialValues={{ title: "", description: "", customerName: "" }}
        submitLabel={createPending ? "Creating" : "Create case"}
        onSubmit={onCreate}
      />
      {!canMutate ? (
        <div className="rounded-md border border-border bg-white p-4 text-sm text-slate-700">
          <Link className="font-medium text-primary" to="/pricing">
            Upgrade to Pro
          </Link>{" "}
          to create, edit, approve, reject, and delete cases.
        </div>
      ) : null}
      {children}
    </section>
  );
}

function CaseListError({ onRetry }: { onRetry(): void }) {
  return (
    <div className="rounded-md border border-red-200 bg-red-50 p-6">
      <h2 className="text-base font-semibold text-red-900">Cases unavailable</h2>
      <p className="mt-2 text-sm text-red-800">Refresh the list or sign in again.</p>
      <button
        type="button"
        className="mt-4 inline-flex h-9 items-center justify-center rounded-md border border-red-300 bg-white px-3 text-sm font-medium text-red-900"
        onClick={onRetry}
      >
        Retry
      </button>
    </div>
  );
}

function CaseListData({
  actionPending,
  canMutate,
  cases,
  editingCaseId,
  editValues,
  updatePending,
  onApprove,
  onCancelEdit,
  onDelete,
  onEdit,
  onReject,
  onUpdate,
}: {
  actionPending: boolean;
  canMutate: boolean;
  cases: CaseSummary[];
  editingCaseId: string | null;
  editValues: CaseMutationValues | null;
  updatePending: boolean;
  onApprove(id: string): void;
  onCancelEdit(): void;
  onDelete(id: string): void;
  onEdit(id: string): void;
  onReject(id: string): void;
  onUpdate(values: CaseMutationValues): void;
}) {
  return (
    <div className="overflow-hidden rounded-md border border-border bg-white">
      <table className="w-full border-collapse text-left text-sm">
        <thead className="bg-slate-100 text-xs uppercase text-slate-600">
          <tr>
            <th className="px-4 py-3 font-semibold">Case</th>
            <th className="px-4 py-3 font-semibold">Customer</th>
            <th className="px-4 py-3 font-semibold">Status</th>
            <th className="px-4 py-3 font-semibold">Updated</th>
            <th className="px-4 py-3 font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody>
          {cases.map((item) => (
            <tr key={item.id} className="border-t border-border align-top">
              {editingCaseId === item.id ? (
                <td className="px-4 py-3" colSpan={5}>
                  {editValues ? (
                    <CaseForm
                      disabled={updatePending}
                      initialValues={editValues}
                      submitLabel={updatePending ? "Saving" : "Save case"}
                      onCancel={onCancelEdit}
                      onSubmit={onUpdate}
                    />
                  ) : (
                    <div className="text-sm text-slate-600">Loading case.</div>
                  )}
                </td>
              ) : (
                <>
                  <td className="px-4 py-3">
                    <div className="font-medium text-slate-950">{item.title}</div>
                    <div className="mt-1 text-xs text-slate-500">{item.reference}</div>
                  </td>
                  <td className="px-4 py-3 text-slate-700">{item.customerName}</td>
                  <td className="px-4 py-3">
                    <span className="inline-flex rounded-md bg-slate-100 px-2 py-1 text-xs font-medium text-slate-700">
                      {item.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-slate-600">{formatDate(item.updatedAt)}</td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap gap-2">
                      <Button
                        variant="outline"
                        className="h-8 px-3"
                        disabled={!canMutate || actionPending}
                        onClick={() => onEdit(item.id)}
                      >
                        Edit
                      </Button>
                      <Button
                        variant="outline"
                        className="h-8 px-3"
                        disabled={!canMutate || actionPending}
                        onClick={() => onApprove(item.id)}
                      >
                        Approve
                      </Button>
                      <Button
                        variant="outline"
                        className="h-8 px-3"
                        disabled={!canMutate || actionPending}
                        onClick={() => onReject(item.id)}
                      >
                        Reject
                      </Button>
                      <Button
                        variant="outline"
                        className="h-8 px-3"
                        disabled={!canMutate || actionPending}
                        onClick={() => onDelete(item.id)}
                      >
                        Delete
                      </Button>
                    </div>
                  </td>
                </>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function CaseForm({
  disabled,
  initialValues,
  submitLabel,
  onCancel,
  onSubmit,
}: {
  disabled: boolean;
  initialValues: CaseMutationValues;
  submitLabel: string;
  onCancel?(): void;
  onSubmit(values: CaseMutationValues): void;
}) {
  const [values, setValues] = useState(initialValues);

  return (
    <form
      className="grid gap-3 rounded-md border border-border bg-white p-4"
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit(values);
        if (!onCancel) {
          setValues({ title: "", description: "", customerName: "" });
        }
      }}
    >
      <div className="grid gap-3 md:grid-cols-2">
        <label className="grid gap-1 text-sm font-medium">
          Title
          <input
            className="h-10 rounded-md border border-border px-3 text-sm outline-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:bg-slate-100"
            disabled={disabled}
            required
            value={values.title}
            onChange={(event) => setValues((current) => ({ ...current, title: event.target.value }))}
          />
        </label>
        <label className="grid gap-1 text-sm font-medium">
          Customer
          <input
            className="h-10 rounded-md border border-border px-3 text-sm outline-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:bg-slate-100"
            disabled={disabled}
            required
            value={values.customerName}
            onChange={(event) => setValues((current) => ({ ...current, customerName: event.target.value }))}
          />
        </label>
      </div>
      <label className="grid gap-1 text-sm font-medium">
        Description
        <textarea
          className="min-h-20 rounded-md border border-border px-3 py-2 text-sm outline-none focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:bg-slate-100"
          disabled={disabled}
          required
          value={values.description}
          onChange={(event) => setValues((current) => ({ ...current, description: event.target.value }))}
        />
      </label>
      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={disabled}>
          {submitLabel}
        </Button>
        {onCancel ? (
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        ) : null}
      </div>
    </form>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(value));
}
