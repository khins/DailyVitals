CREATE OR REPLACE FUNCTION public.update_retail_price(
    p_retail_price_id int,
    p_item_id int,
    p_store_id int,
    p_retail_price numeric,
    p_price_date timestamp,
    p_is_sale_price bool,
    p_notes text,
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

    UPDATE public.retail_prices
    SET
        item_id = p_item_id,
        store_id = p_store_id,
        retail_price = p_retail_price,
        price_date = p_price_date,
        is_sale_price = COALESCE(p_is_sale_price, false),
        notes = p_notes,
        updated_at = CURRENT_TIMESTAMP
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
        'UPDATE',
        p_entered_by,
        jsonb_build_object(
            'before', v_old,
            'after', jsonb_build_object(
                'item_id', p_item_id,
                'store_id', p_store_id,
                'retail_price', p_retail_price,
                'price_date', p_price_date,
                'is_sale_price', p_is_sale_price,
                'notes', p_notes
            )
        )
    );
END;
$$;
