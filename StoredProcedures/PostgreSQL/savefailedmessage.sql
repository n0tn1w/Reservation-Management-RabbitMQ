CREATE OR REPLACE FUNCTION savefailedmessage (RawRequest TEXT)
RETURNS VOID AS $$
BEGIN
        INSERT INTO postgres.public.FailedMessages (Raw_request)
        VALUES (RawRequest);

EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        RAISE EXCEPTION 'An error occurred: %', SQLERRM;
END;
$$ LANGUAGE plpgsql;
