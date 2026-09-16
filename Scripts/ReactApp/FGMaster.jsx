const { useState } = React;

function FGMaster() {

    const [formData, setFormData] = useState({
        FGCode: "",
        FGName: "",
        FGDesc: "",
        LockStatus: "Y"
    });

    const [errors, setErrors] = useState({});
    const [loading, setLoading] = useState(false);
    const [successMsg, setSuccessMsg] = useState("");

    const handleChange = (e) => {

        setFormData({
            ...formData,
            [e.target.name]: e.target.value
        });

        // clear error when user types
        setErrors({
            ...errors,
            [e.target.name]: ""
        });
    };

    const validate = () => {

        let err = {};

        if (!formData.FGCode.trim())
            err.FGCode = "FG Code is required";

        if (!formData.FGName.trim())
            err.FGName = "FG Name is required";

        return err;
    };

    const handleSubmit = async (e) => {

        e.preventDefault();

        setSuccessMsg("");

        const err = validate();

        if (Object.keys(err).length > 0) {
            setErrors(err);
            return;
        }

        setLoading(true);

        try {

            const response = await fetch('/FGMaster/SaveData', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {

                setSuccessMsg(result.message);

                setFormData({
                    FGCode: "",
                    FGName: "",
                    FGDesc: "",
                    LockStatus: "Y"
                });

            } else {
                setSuccessMsg(result.message);
            }

        } catch (err) {

            setSuccessMsg("Something went wrong");

        } finally {
            setLoading(false);
        }
    };

    return (

        <div className="card shadow p-4">

            <h3 className="mb-3">FG Master</h3>

            {successMsg && (
                <div className="alert alert-info">
                    {successMsg}
                </div>
            )}

            <form onSubmit={handleSubmit}>

                {/* FG Code */}
                <div className="mb-3">
                    <label>FG Code</label>
                    <input
                        type="text"
                        className="form-control"
                        name="FGCode"
                        value={formData.FGCode}
                        onChange={handleChange}
                    />
                    <small className="text-danger">{errors.FGCode}</small>
                </div>

                {/* FG Name */}
                <div className="mb-3">
                    <label>FG Name</label>
                    <input
                        type="text"
                        className="form-control"
                        name="FGName"
                        value={formData.FGName}
                        onChange={handleChange}
                    />
                    <small className="text-danger">{errors.FGName}</small>
                </div>

                {/* FG Desc */}
                <div className="mb-3">
                    <label>FG Desc</label>
                    <textarea
                        className="form-control"
                        name="FGDesc"
                        value={formData.FGDesc}
                        onChange={handleChange}
                    />
                </div>

                {/* Lock Status */}
                <div className="mb-3">
                    <label>Lock Status</label>
                    <select
                        className="form-control"
                        name="LockStatus"
                        value={formData.LockStatus}
                        onChange={handleChange}
                    >
                        <option value="Y">Y</option>
                        <option value="N">N</option>
                    </select>
                </div>

                <button
                    className="btn btn-primary"
                    disabled={loading}
                >
                    {loading ? "Saving..." : "Save"}
                </button>

            </form>

        </div>
    );
}

const root = ReactDOM.createRoot(
    document.getElementById("root")
);

root.render(<FGMaster />);