import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { productsApi } from "../api";
import type { CreateProductInput, Product, UpdateProductInput } from "../types";
import { Badge, Button, EmptyState, Input, Modal, Spinner } from "../shared/ui";
import { ProductForm } from "./ProductForm";

const PAGE_SIZE = 10;

export function AdminProductsPage() {
  const qc = useQueryClient();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [deleting, setDeleting] = useState<Product | null>(null);

  const listKey = ["products", { page, pageSize: PAGE_SIZE, search }] as const;

  const { data, isLoading, isError, error } = useQuery({
    queryKey: listKey,
    queryFn: () => productsApi.list({ page, pageSize: PAGE_SIZE, search }),
  });

  const invalidate = () =>
    qc.invalidateQueries({ queryKey: ["products"] });

  const createMut = useMutation({
    mutationFn: (input: CreateProductInput) => productsApi.create(input),
    onSuccess: () => {
      invalidate();
      setCreateOpen(false);
    },
  });

  const updateMut = useMutation({
    mutationFn: (input: UpdateProductInput) => productsApi.update(input),
    onSuccess: () => {
      invalidate();
      setEditing(null);
    },
  });

  const deleteMut = useMutation({
    mutationFn: (id: string) => productsApi.delete(id),
    onSuccess: () => {
      invalidate();
      setDeleting(null);
    },
  });

  return (
   <div className="mx-auto min-h-screen max-w-7xl px-6 py-10">
<header className="mb-6 flex items-center justify-between rounded-3xl border border-white/70 bg-white/70 px-8 py-6 shadow-lg backdrop-blur">
        <div className="space-y-1">
          <h1 className="text-3xl font-bold text-zinc-900">Products</h1>
          <p className="mt-1 text-sm text-zinc-500">
            Manage the product catalog served by <code className="rounded bg-zinc-100 px-1.5 py-0.5 text-xs">ProductService.Api</code>.
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)}>+ New product</Button>
      </header>

      <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <Input
          placeholder="Search by name…"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
          className="max-w-xs"
        />
        {data && (
          <span className="text-sm text-zinc-500">
            {data.totalCount} total
          </span>
        )}
      </div>

      {isLoading && <Spinner />}

      {isError && (
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          <strong className="font-semibold">Failed to load products.</strong>{" "}
          {error instanceof Error ? error.message : "Unknown error"}
          <p className="mt-1 text-xs text-red-700">
            Make sure the API is running at https://localhost:7080 and that you've
            accepted the dev certificate.
          </p>
        </div>
      )}

      {data && data.items.length === 0 && !isLoading && (
        <EmptyState
          title="No products yet"
          hint={search ? "Try a different search term." : "Click “+ New product” to add one."}
        />
      )}

      {data && data.items.length > 0 && (
       <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-xl shadow-slate-200/70">
          <table className="min-w-full divide-y divide-zinc-200 text-sm">
            <thead className="bg-slate-900 text-left text-xs font-semibold uppercase tracking-wide text-white">
              <tr>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Price</th>
                <th className="px-4 py-3">Stock</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {data.items.map((p) => (
                <tr key={p.id} className="transition hover:bg-slate-50">
                  <td className="px-4 py-3">
                    <div className="font-medium text-zinc-900">{p.name}</div>
                    {p.description && (
                      <div className="mt-0.5 line-clamp-1 text-xs text-zinc-500">
                        {p.description}
                      </div>
                    )}
                  </td>
                  <td className="px-4 py-3 text-zinc-700">
                    {new Intl.NumberFormat(undefined, {
                      style: "currency",
                      currency: p.currency,
                    }).format(p.price)}
                  </td>
                  <td className="px-4 py-3 text-zinc-700">{p.stockQuantity}</td>
                  <td className="px-4 py-3">
                    {p.isActive ? (
                      <Badge tone="success">Active</Badge>
                    ) : (
                      <Badge tone="danger">Inactive</Badge>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="ghost" onClick={() => setEditing(p)}>
                        Edit
                      </Button>
                      <Button variant="danger" onClick={() => setDeleting(p)}>
                        Delete
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {data.totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-zinc-200 bg-zinc-50 px-4 py-3 text-sm">
              <span className="text-zinc-600">
                Page {data.page} of {data.totalPages}
              </span>
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  disabled={!data.hasPrevious}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  ← Previous
                </Button>
                <Button
                  variant="secondary"
                  disabled={!data.hasNext}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Next →
                </Button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Create modal */}
      <Modal open={createOpen} onClose={() => setCreateOpen(false)} title="New product">
        <ProductForm
          submitting={createMut.isPending}
          onCancel={() => setCreateOpen(false)}
          onSubmit={(values) => createMut.mutate(values)}
        />
        {createMut.isError && (
          <p className="mt-3 text-sm text-red-600">
            {createMut.error instanceof Error ? createMut.error.message : "Failed to create."}
          </p>
        )}
      </Modal>

      {/* Edit modal */}
      <Modal open={editing !== null} onClose={() => setEditing(null)} title="Edit product">
        {editing && (
          <>
            <ProductForm
              initial={editing}
              submitting={updateMut.isPending}
              onCancel={() => setEditing(null)}
              onSubmit={(values) =>
                updateMut.mutate({
                  id: editing.id,
                  name: values.name,
                  description: values.description,
                  price: values.price,
                  currency: values.currency,
                  stockQuantity: values.stockQuantity,
                })
              }
            />
            {updateMut.isError && (
              <p className="mt-3 text-sm text-red-600">
                {updateMut.error instanceof Error ? updateMut.error.message : "Failed to update."}
              </p>
            )}
          </>
        )}
      </Modal>

      {/* Delete confirmation modal */}
      <Modal open={deleting !== null} onClose={() => setDeleting(null)} title="Delete product?">
        {deleting && (
          <>
            <p className="text-sm text-zinc-700">
              This will permanently remove <strong>{deleting.name}</strong>. This action cannot be undone.
            </p>
            <div className="mt-5 flex justify-end gap-2">
              <Button variant="secondary" onClick={() => setDeleting(null)} disabled={deleteMut.isPending}>
                Cancel
              </Button>
              <Button
                variant="danger"
                disabled={deleteMut.isPending}
                onClick={() => deleteMut.mutate(deleting.id)}
              >
                {deleteMut.isPending ? "Deleting…" : "Delete"}
              </Button>
            </div>
          </>
        )}
      </Modal>
    </div>
  );
}
