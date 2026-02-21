CREATE OR REPLACE FUNCTION public.insert_retail_price(
    p_item_id int,
    p_store_id int,
    p_retail_price numeric,
    p_price_date timestamp,
    p_is_sale_price bool,
    p_notes text,
    p_entered_by varchar
)
RETURNS int
LANGUAGE plpgsql
AS $$
DECLARE
    v_retail_price_id int;
BEGIN
    INSERT INTO public.retail_prices (
        item_id,
        store_id,
        retail_price,
        price_date,
        is_sale_price,
        notes
    )
    VALUES (
        p_item_id,
        p_store_id,
        p_retail_price,
        p_price_date,
        COALESCE(p_is_sale_price, false),
        p_notes
    )
    RETURNING retail_price_id
    INTO v_retail_price_id;

    INSERT INTO public.data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'retail_prices',
        v_retail_price_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'item_id', p_item_id,
            'store_id', p_store_id,
            'retail_price', p_retail_price,
            'price_date', p_price_date,
            'is_sale_price', p_is_sale_price,
            'notes', p_notes
        )
    );

    RETURN v_retail_price_id;
END;
$$;
