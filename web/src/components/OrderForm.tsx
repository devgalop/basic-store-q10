import { useId, useState, type ChangeEvent, type FormEvent } from 'react';
import { ApiValidationError, createOrder } from '../api/ordersApi';
import type { FieldErrors, FieldName, Order } from '../api/types';
import { MAX_QUANTITY, MIN_QUANTITY, hasErrors, validateOrderForm, type OrderFormValues } from '../validation';

interface Props {
  onCreated: (order: Order) => void;
}

const EMPTY: OrderFormValues = { customerName: '', sku: '', quantity: '1' };

interface TextFieldProps {
  label: string;
  name: FieldName;
  value: string;
  errors: string[] | undefined;
  onChange: (event: ChangeEvent<HTMLInputElement>) => void;
  hint?: string;
  inputMode?: 'numeric';
  disabled: boolean;
}

function TextField({ label, name, value, errors, onChange, hint, inputMode, disabled }: TextFieldProps) {
  const id = useId();
  const messageId = `${id}-msg`;
  const invalid = !!errors?.length;

  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        id={id}
        name={name}
        value={value}
        onChange={onChange}
        inputMode={inputMode}
        disabled={disabled}
        autoComplete="off"
        aria-invalid={invalid}
        aria-describedby={invalid || hint ? messageId : undefined}
      />
      <div id={messageId}>
        {invalid ? (
          errors.map((message) => (
            <p key={message} className="field-error">
              {message}
            </p>
          ))
        ) : hint ? (
          <p className="muted">{hint}</p>
        ) : null}
      </div>
    </div>
  );
}

export function OrderForm({ onCreated }: Props) {
  const [values, setValues] = useState<OrderFormValues>(EMPTY);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const change = (field: FieldName) => (event: ChangeEvent<HTMLInputElement>) => {
    setValues((current) => ({ ...current, [field]: event.target.value }));
    // Al corregir un campo, su error deja de mostrarse (se vuelve a validar al enviar).
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (submitting) return;

    setFormError(null);
    const clientErrors = validateOrderForm(values);
    setErrors(clientErrors);
    if (hasErrors(clientErrors)) return;

    setSubmitting(true);
    try {
      const order = await createOrder({
        customerName: values.customerName.trim(),
        sku: values.sku.trim(),
        quantity: Number(values.quantity),
      });
      setValues(EMPTY);
      setErrors({});
      onCreated(order);
    } catch (error) {
      if (error instanceof ApiValidationError) {
        setErrors(error.fieldErrors); // el servidor es la autoridad: ej. un SKU que no existe en el catálogo
      } else {
        setFormError(error instanceof Error ? error.message : 'Ocurrió un error inesperado.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    // noValidate: la validación nativa del navegador taparía los mensajes visibles de arriba.
    <form onSubmit={handleSubmit} noValidate aria-labelledby="form-title">
      <h2 id="form-title">Nuevo pedido</h2>

      {formError && (
        <p className="banner banner-error" role="alert">
          {formError}
        </p>
      )}
      {errors.general?.map((message) => (
        <p key={message} className="banner banner-error" role="alert">
          {message}
        </p>
      ))}

      <TextField
        label="Cliente"
        name="customerName"
        value={values.customerName}
        errors={errors.customerName}
        onChange={change('customerName')}
        disabled={submitting}
      />
      <TextField
        label="SKU"
        name="sku"
        value={values.sku}
        errors={errors.sku}
        onChange={change('sku')}
        hint="Ej.: ABC-01 o XYZ-02 (con stock), LMN-03 (stock 5), OUT-04 (sin stock: el pedido será rechazado)."
        disabled={submitting}
      />
      <TextField
        label="Cantidad"
        name="quantity"
        value={values.quantity}
        errors={errors.quantity}
        onChange={change('quantity')}
        inputMode="numeric"
        hint={`Entre ${MIN_QUANTITY} y ${MAX_QUANTITY}.`}
        disabled={submitting}
      />

      <button type="submit" disabled={submitting}>
        {submitting ? 'Enviando…' : 'Crear pedido'}
      </button>
    </form>
  );
}
