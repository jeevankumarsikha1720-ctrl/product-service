import { useForm } from "react-hook-form";
import type { CreateProductInput, Product } from "../types";
import { Button, FieldError, Input, Label } from "../shared/ui";

const CURRENCIES = ["USD", "EUR", "GBP", "INR", "AUD", "CAD"] as const;

interface Props {
  initial?: Product;
  submitting?: boolean;
  onSubmit: (values: CreateProductInput) => void;
  onCancel: () => void;
}

export function ProductForm({ initial, submitting, onSubmit, onCancel }: Props) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CreateProductInput>({
    defaultValues: initial
      ? {
          name: initial.name,
          description: initial.description,
          price: initial.price,
          currency: initial.currency,
          stockQuantity: initial.stockQuantity,
        }
      : {
          name: "",
          description: "",
          price: 0,
          currency: "USD",
          stockQuantity: 0,
        },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <Label>Name</Label>
        <Input
          {...register("name", {
            required: "Name is required",
            maxLength: { value: 200, message: "Max 200 characters" },
          })}
        />
        <FieldError message={errors.name?.message} />
      </div>

      <div>
        <Label>Description</Label>
        <textarea
          rows={3}
          className="w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          {...register("description", {
            maxLength: { value: 2000, message: "Max 2000 characters" },
          })}
        />
        <FieldError message={errors.description?.message} />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <Label>Price</Label>
          <Input
            type="number"
            step="0.01"
            min="0"
            {...register("price", {
              required: "Price is required",
              valueAsNumber: true,
              min: { value: 0, message: "Price must be ≥ 0" },
            })}
          />
          <FieldError message={errors.price?.message} />
        </div>
        <div>
          <Label>Currency</Label>
          <select
            className="w-full rounded-md border border-zinc-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            {...register("currency", { required: true })}
          >
            {CURRENCIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div>
        <Label>Stock quantity</Label>
        <Input
          type="number"
          min="0"
          {...register("stockQuantity", {
            required: "Stock is required",
            valueAsNumber: true,
            min: { value: 0, message: "Stock must be ≥ 0" },
          })}
        />
        <FieldError message={errors.stockQuantity?.message} />
        {initial && (
          <p className="mt-1 text-xs text-slate-500">
            Current stock: {initial.stockQuantity}. Set any non-negative value to adjust.
          </p>
        )}
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="secondary" onClick={onCancel} disabled={submitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? "Saving…" : initial ? "Save changes" : "Create product"}
        </Button>
      </div>
    </form>
  );
}
