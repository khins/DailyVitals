CREATE OR REPLACE FUNCTION public.delete_retail_price(
    p_retail_price_id int,
    p_entered_by varchar
)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_old jsonb;
BEGIN
    SELECT jsonb_build_object(
        'item_id', item_id,
        'store_id', store_id,
        'retail_price', retail_price,
        'price_date', price_date,
        'is_sale_price', is_sale_price,
        'notes', notes
    )
    INTO v_old
    FROM retail_prices
    WHERE retail_price_id = p_retail_price_id;

    DELETE FROM public.retail_prices
    WHERE retail_price_id = p_retail_price_id;

    INSERT INTO public.data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'retail_prices',
        p_retail_price_id,
        'DELETE',
        p_entered_by,
        v_old
    );
END;
$$;
